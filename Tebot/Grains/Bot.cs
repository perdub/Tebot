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
        protected ITelegramBotClient BotClient;
        protected internal IPersistentState<TState>? State;
        protected TState Data => State?.State!;

        protected Task SaveAsync()
        {
            if (State != null)
            {
                return State.WriteStateAsync();
            }
            return Task.CompletedTask;
        }
        protected Task ClearStateAsync()
        {
            if (State != null) {
                return State.ClearStateAsync();
            }
            return Task.CompletedTask;
        }

        protected ILogger<TImplementation>? Logger;

        protected Update? currentUpdate;

        protected long ChatId => this.GetPrimaryKeyLong();
        protected string Text => currentUpdate!.Message!.Text ?? currentUpdate.Message.Caption;
        public async ValueTask SendUpdate(Immutable<Update> update)
        {
            currentUpdate = update.Value;

            await invokeCallbacks(currentUpdate);

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

        public override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            parceImplementation();
            BotClient = ServiceProvider.GetRequiredService<ITelegramBotClient>();

            var persistenceFactory = ServiceProvider.GetRequiredService<IPersistentStateFactory>()!;
            State = persistenceFactory.Create<TState>(GrainContext, new PersistentStateConfigurationImpl(
                StateName,
                StorageName));

            await State.ReadStateAsync(cancellationToken);

            if(State.State is null)
            {
                State.State = new TState();
            }

            Logger = ServiceProvider.GetRequiredService<ILogger<TImplementation>>();

            await base.OnActivateAsync(cancellationToken);
            return;
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
                        var idIndex = text.IndexOf('@');
                        if(idIndex != -1)
                            text = text.Substring(0, text.IndexOf('@'));
                        return (true, text);
                    }
                }
            }

            return (false, null);
        }


        private async Task invokeCallbacks(Update update)
        {
            await OnUpdateReceived(update);

            if (update.Message is not null)
            {
                await OnMessageReceived(update.Message);
            }

            if(update.InlineQuery is not null)
            {
                await OnInlineQueryRequest(update.InlineQuery);
            }

            if (update.ChosenInlineResult is not null) {
                await OnInlineChosenResult(update.ChosenInlineResult);
            }
        }

        protected virtual Task OnMessageReceived(Message message) { return Task.CompletedTask; }
        protected virtual Task OnUpdateReceived(Update update) { return Task.CompletedTask; }
        protected virtual Task OnInlineQueryRequest(InlineQuery inlineQuery) { return Task.CompletedTask; }
        protected virtual Task OnInlineChosenResult(ChosenInlineResult result) { return Task.CompletedTask; }

        public static string StateName { get; set; } = "bot-states";
        public static string StorageName { get; set; } = typeof(TState).Name;
        private void parceImplementation()
        {
            ImplMap<TImplementation>.ParseType();
        }
    }
}
