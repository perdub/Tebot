using System;

namespace Tebot;

[AttributeUsage(AttributeTargets.Method)]
public class StateIdAttribute : Attribute
{
    public string? State {get;set;}
}
