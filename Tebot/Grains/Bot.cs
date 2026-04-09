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
    /// <summary>
    /// Base class for implementing a Telegram bot with state machine pattern using Orleans grains.
    /// Each user interaction is handled by a separate grain instance with persistent state.
    /// </summary>
    /// <typeparam name="TImplementation">The concrete bot implementation type.</typeparam>
    /// <typeparam name="TState">The state type that inherits from <see cref="BotState"/>.</typeparam>
    public abstract partial class Bot<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] TImplementation, TState> : Grain, IBotGrain
        where TImplementation : Bot<TImplementation, TState>
        where TState : BotState, new()
    {
        /// <summary>
        /// Gets the Telegram Bot API client for sending messages and interacting with Telegram.
        /// </summary>
        protected ITelegramBotClient BotClient;
        
        /// <summary>
        /// Gets the persistent state storage for this grain.
        /// </summary>
        protected internal IPersistentState<TState>? State;
        
        /// <summary>
        /// Gets the current user's state data.
        /// </summary>
        protected TState Data => State?.State!;
        
        /// <summary>
        /// Transitions the bot to a new state.
        /// </summary>
        /// <param name="state">The name of the target state.</param>
        /// <param name="perfomSaving">If true, automatically saves the state after transition. Default is true.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected async Task GoToState(string state, bool perfomSaving = true)
        {
            State!.State.State = state;
            if (perfomSaving)
            {
                await SaveAsync();
            }
        }

        /// <summary>
        /// Saves the current state to persistent storage.
        /// </summary>
        /// <returns>A task representing the asynchronous save operation.</returns>
        protected Task SaveAsync()
        {
            if (State != null)
            {
                return State.WriteStateAsync();
            }
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Clears the current state from persistent storage.
        /// </summary>
        /// <returns>A task representing the asynchronous clear operation.</returns>
        protected Task ClearStateAsync()
        {
            if (State != null) {
                return State.ClearStateAsync();
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets the logger instance for this bot.
        /// </summary>
        protected ILogger<TImplementation>? Logger;

        /// <summary>
        /// Gets the current Telegram update being processed.
        /// </summary>
        protected Update? currentUpdate;

        /// <summary>
        /// Gets the chat ID for the current user interaction.
        /// </summary>
        protected long ChatId => this.GetPrimaryKeyLong();
        
        /// <summary>
        /// Gets the text content from the current message (either Text or Caption).
        /// </summary>
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
            BotClient = ServiceProvider.GetRequiredService<ITelegramBotClient>();
            parceImplementation();

            var persistenceFactory = ServiceProvider.GetRequiredService<IPersistentStateFactory>()!;
            State = persistenceFactory.Create<TState>(GrainContext, new PersistentStateConfigurationImpl(
                DbStateName,
                DbStorageName));

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

        /// <summary>
        /// Called when a message is received. Override this method to handle all incoming messages.
        /// </summary>
        /// <param name="message">The received message.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected virtual Task OnMessageReceived(Message message) { return Task.CompletedTask; }
        
        /// <summary>
        /// Called when any update is received. Override this method to handle all types of updates.
        /// </summary>
        /// <param name="update">The received update.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected virtual Task OnUpdateReceived(Update update) { return Task.CompletedTask; }
        
        /// <summary>
        /// Called when an inline query is received. Override this method to handle inline queries.
        /// </summary>
        /// <param name="inlineQuery">The received inline query.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected virtual Task OnInlineQueryRequest(InlineQuery inlineQuery) { return Task.CompletedTask; }
        
        /// <summary>
        /// Called when a user selects an inline query result. Override this method to handle chosen inline results.
        /// </summary>
        /// <param name="result">The chosen inline result.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected virtual Task OnInlineChosenResult(ChosenInlineResult result) { return Task.CompletedTask; }

        /// <summary>
        /// Gets or sets the database state name used for persistence.
        /// </summary>
        public static string DbStateName { get; set; } = "bot-states";
        
        /// <summary>
        /// Gets or sets the database storage name used for persistence.
        /// </summary>
        public static string DbStorageName { get; set; } = typeof(TState).Name;
        private void parceImplementation()
        {
            ImplMap<TImplementation>.ParseType(BotClient);
        }
    }
}
