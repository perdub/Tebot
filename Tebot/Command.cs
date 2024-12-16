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

        public 
        #if NETSTANDARD2_0
            string
            #else
            string?
            #endif
        Description {get;set;} = null;

        public CommandAttribute(string command, InvokeMode invokeMode = InvokeMode.Async, string description = null)
        {
            this.Command = command;
            this.InvokeMode = invokeMode;
            this.Description = description;
        }
        public CommandAttribute()
        {

        }
    }
}
