using System;

namespace Tebot;

[AttributeUsage(AttributeTargets.Method)]
public class StateIdAttribute : Attribute
{
    public string? State {get;set;}

    public StateIdAttribute(string State){
        this.State = State;
    }
    public StateIdAttribute()
    {
        
    }
}
