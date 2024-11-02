using System;

namespace Tebot;

[AttributeUsage(AttributeTargets.Method)]
public class CommandAttribute : Attribute
{
    public string? Command {get;set;}

    public CommandAttribute(string command){
        this.Command = command;
    }
    public CommandAttribute()
    {
        
    }
}
