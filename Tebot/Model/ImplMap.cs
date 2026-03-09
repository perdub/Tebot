using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Tebot.Attributes;
using Tebot.Grains;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Tebot.Model
{
    internal static class ImplMap<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] TImplementation>
    {
        static internal Dictionary<string, MethodInfo> States { get; set; } = new Dictionary<string, MethodInfo>();
        static internal Dictionary<string, MethodInfo> Commands { get; set; } = new Dictionary<string, MethodInfo>();

        static internal Dictionary<string, string?> CommandDescriptions { get; set; } = new Dictionary<string, string?>();

        static internal bool IsParsed = false;
        static readonly object _lock = new object();

        internal static void ParseType(ITelegramBotClient botClient)
        {
            if (ImplMap<TImplementation>.IsParsed)
            {
                //already parsed
                return;
            }
            lock (_lock)
            {
                if (ImplMap<TImplementation>.IsParsed)
                {
                    //already parsed
                    return;
                }

                var typeObject = typeof(TImplementation);
                var typeMethods = typeObject.GetMethods(BindingFlags.Public | BindingFlags.Instance);

                var states = typeMethods.Select(a => new MethodAttributePair<StateAttribute>(a, a.GetCustomAttribute<StateAttribute>()))
                    .Where(b => b.attribute is not null);
                var commands = typeMethods.Select(a => new MethodAttributePair<CommandAttribute>(a, a.GetCustomAttribute<CommandAttribute>()))
                    .Where(b => b.attribute is not null);

                foreach (var s in states)
                {
                    States[s.attribute.Value] = s.methodInfo;
                }
                foreach (var s in commands)
                {
                    Commands[s.attribute.Value] = s.methodInfo;

                    CommandDescriptions[s.attribute.Value] = s.attribute.CommandDescription;
                }

                //not sure if this should be there
                var botCommands = CommandDescriptions.Where(a => a.Value is not null).Select(b => new BotCommand(b.Key, b.Value));
                botClient.SetMyCommands(botCommands);

                ImplMap<TImplementation>.IsParsed = true;
            }
        }

        private record MethodAttributePair<TAttribute>(MethodInfo methodInfo, TAttribute? attribute) where TAttribute : Attribute;
    }

}

