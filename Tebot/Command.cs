using System;

namespace Tebot;

[AttributeUsage(AttributeTargets.Method)]
public class CommandAttribute : Attribute
{
    public string? Command {get;set;}
    public InvokeMode InvokeMode {get;set;}

    public CommandAttribute(string command, InvokeMode invokeMode = InvokeMode.Async){
        this.Command = command;
        this.InvokeMode = invokeMode;
    }
    public CommandAttribute()
    {
        
    }
}