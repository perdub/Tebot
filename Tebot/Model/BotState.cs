using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tebot.Model
{
    /// <summary>
    /// Base class for bot state. Inherit from this class to define custom state properties for your bot.
    /// </summary>
    /// <example>
    /// <code>
    /// public class MyBotState : BotState
    /// {
    ///     public int Counter { get; set; } = 0;
    ///     public string? UserName { get; set; }
    /// }
    /// </code>
    /// </example>
    public class BotState
    {
        /// <summary>
        /// Gets or sets the current state name. Default is "start".
        /// </summary>
        public string State { get; set; } = "start";
    }
}
