using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tebot.Model
{
    /// <summary>
    /// Configuration options for building a Tebot application.
    /// </summary>
    public record TebotConfig
    {
        /// <summary>
        /// Gets or sets the Orleans storage name for grain storage. Default is "bot-data".
        /// </summary>
        public String StorageName { get; set; } = "bot-data";
        
        /// <summary>
        /// Gets or sets the state type name for persistence. Default is "my-state".
        /// </summary>
        public String StateName { get; set; } = "my-state";

        /// <summary>
        /// Gets or sets the command line arguments passed to the application.
        /// </summary>
        public string[] ConsoleArguments { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets an optional callback to configure the application's configuration manager.
        /// Use this to add configuration sources like JSON files or environment variables.
        /// </summary>
        /// <example>
        /// <code>
        /// ProcessConfigurationManager = (manager) => {
        ///     manager.AddJsonFile("config.json");
        /// }
        /// </code>
        /// </example>
        public Action<ConfigurationManager>? ProcessConfigurationManager { get; set; } = null;
    }
}
