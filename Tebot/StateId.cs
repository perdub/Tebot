using System;

namespace Tebot
{

    [AttributeUsage(AttributeTargets.Method)]
    public class StateIdAttribute : Attribute
    {
        public 
        #if NETSTANDARD2_0
            string
            #else
            string?
            #endif
         State { get; set; }
        public InvokeMode InvokeMode { get; set; }

        public StateIdAttribute(string State, InvokeMode invokeMode = InvokeMode.Sync)
        {
            this.State = State;
            this.InvokeMode = invokeMode;
        }
        public StateIdAttribute()
        {

        }
    }
}