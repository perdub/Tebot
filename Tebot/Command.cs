using System;

namespace Tebot
{

    [AttributeUsage(AttributeTargets.Method)]
    public class CommandAttribute : Attribute
    {
        public 
        #if NETSTANDARD2_0
            string
            #else
            string?
            #endif
         Command { get; set; }
        public InvokeMode InvokeMode { get; set; }

        public CommandAttribute(string command, InvokeMode invokeMode = InvokeMode.Async)
        {
            this.Command = command;
            this.InvokeMode = invokeMode;
        }
        public CommandAttribute()
        {

        }
    }
}
