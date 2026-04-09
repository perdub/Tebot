using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tebot.Attributes
{
    /// <summary>
    /// Marks a method as a Telegram bot command handler.
    /// Commands with descriptions are automatically registered in Telegram via SetMyCommands.
    /// </summary>
    /// <example>
    /// <code>
    /// [Command("/start", "Start the bot")]
    /// public async Task StartCommand()
    /// {
    ///     await BotClient.SendMessage(ChatId, "Welcome!");
    /// }
    /// </code>
    /// </example>
    [System.AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class CommandAttribute : ValueAttribute
    {
        /// <summary>
        /// Gets the description of the command that will be shown to users in Telegram.
        /// </summary>
        public string? CommandDescription { get; private set; } = null;

        /// <summary>
        /// Gets a value indicating whether this command is private (not registered in Telegram menu).
        /// </summary>
        public bool IsPrivateCommand => string.IsNullOrWhiteSpace(CommandDescription);
        
        /// <summary>
        /// Initializes a new instance of the <see cref="CommandAttribute"/> class.
        /// </summary>
        /// <param name="state">The command string (e.g., "/start").</param>
        /// <param name="description">The description shown in Telegram. Leave empty for private commands.</param>
        public CommandAttribute(string state, string description) : base(state)
        {
            CommandDescription = description;
        }
    }
}
