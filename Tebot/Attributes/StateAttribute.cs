using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tebot.Attributes
{
    /// <summary>
    /// Marks a method as a state handler in the bot's state machine.
    /// When the bot's current state matches the specified state name, this method will be invoked.
    /// </summary>
    /// <example>
    /// <code>
    /// [State("start")]
    /// public async Task Start()
    /// {
    ///     await BotClient.SendMessage(ChatId, "Hello!");
    /// }
    /// </code>
    /// </example>
    [System.AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class StateAttribute : ValueAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StateAttribute"/> class.
        /// </summary>
        /// <param name="state">The name of the state this method handles.</param>
        public StateAttribute(string state):base(state)
        {
        }
    }
}
