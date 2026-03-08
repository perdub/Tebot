using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Tebot.Model;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Tebot.Grains
{
    public abstract partial class Bot<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] TImplementation, TState> : Grain, IBotGrain
        where TImplementation : Bot<TImplementation, TState>
        where TState : BotState, new()
    {
        protected ITelegramBotClient? BotClient;
        protected IPersistentState<TState>? State;

        protected ILogger<TImplementation>? Logger;

        protected Update? currentUpdate;

        protected long ChatId => this.GetPrimaryKeyLong();
        protected string Text => currentUpdate!.Message!.Text ?? currentUpdate.Message.Caption;
        public async ValueTask SendUpdate(Immutable<Update> update)
        {
            currentUpdate = update.Value;

            //check commands
            var command = isCommand();
            if (command.isCommand)
            {
                bool commandFound = ImplMap<TImplementation>.Commands.TryGetValue(command.command!, out var commandInfo);
                if (commandFound)
                {
                    var methodResult = commandInfo.Invoke(this, Array.Empty<object>());
                    await getTask(methodResult);

                    return;
                }
                else {
                    Logger?.LogWarning("Command {0} requested, but not found", command.command!);
                }
            }

            //invoke main state.
            // its fucking disaster
            var isStateFound = ImplMap<TImplementation>.States.TryGetValue(State!.State.State, out var stateMethodInfo);
            if (!isStateFound)
            {
                Logger?.LogError("State {0} not found.", State!.State.State);
                return;
            }

            var stateExecutionResult = stateMethodInfo!.Invoke(this, Array.Empty<object>());
            await getTask(stateExecutionResult);
        }

        public override Task OnActivateAsync(CancellationToken cancellationToken)
        {
            parceImplementation();
            BotClient = ServiceProvider.GetRequiredService<ITelegramBotClient>();

            var persistenceFactory = ServiceProvider.GetRequiredService<IPersistentStateFactory>()!;
            State = persistenceFactory.Create<TState>(GrainContext, new PersistentStateConfigurationImpl(
                StateName,
                StorageName));
            State.State = new ();

            Logger = ServiceProvider.GetRequiredService<ILogger<TImplementation>>();

            return base.OnActivateAsync(cancellationToken);
        }


        private Task getTask(object? methodResult)
        {
            if (methodResult is Task taskResult)
            {
                return taskResult;
            }
            return Task.CompletedTask;
        }
        private (bool isCommand, string? command) isCommand()
        {
            if (currentUpdate is not null) {
                if(currentUpdate.Message is not null)
                {
                    string text;
                    if(currentUpdate.Message.Text is not null)
                    {
                        text = currentUpdate.Message.Text;
                    }
                    else if (currentUpdate.Message.Caption is not null)
                    {
                        text = currentUpdate.Message.Caption;
                    }
                    else
                    {
                        return (false, null);
                    }

                    if (text.StartsWith('/'))
                    {
                        text = text.Substring(0, text.IndexOf('@'));
                        return (true, text);
                    }
                }
            }

            return (false, null);
        }


        public static string StateName { get; set; } = "bot-states";
        public static string StorageName { get; set; } = typeof(TState).Name;
        private void parceImplementation()
        {
            ImplMap<TImplementation>.ParseType();
        }
    }
}
