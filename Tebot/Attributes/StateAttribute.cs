using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tebot.Attributes
{
    [System.AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class StateAttribute : ValueAttribute
    {
        public StateAttribute(string state):base(state)
        {
        }
    }
}
