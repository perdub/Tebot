using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tebot.Attributes
{
    [System.AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public abstract class CommandAttribute : ValueAttribute
    {
        public string CommandDescription { get; private set; }

        public bool IsPrivateCommand => string.IsNullOrWhiteSpace(CommandDescription);
        public CommandAttribute(string state, string description) : base(state)
        {
            CommandDescription = description;
        }
    }
}
