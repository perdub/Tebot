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
        public BehaviourAfterCommand Behaviour { get; set; }
        public 
        #if NETSTANDARD2_0
            string
            #else
            string?
            #endif
        Description {get;set;} = null;

        public CommandAttribute(string command, BehaviourAfterCommand behaviour = BehaviourAfterCommand.Continue, InvokeMode invokeMode = InvokeMode.Async, string description = null)
        {
            this.Command = command;
            this.InvokeMode = invokeMode;
            this.Description = description;
            this.Behaviour = behaviour;
        }
        public CommandAttribute()
        {

        }
    }

    public enum BehaviourAfterCommand{
        Continue,
        Break
    }
}
