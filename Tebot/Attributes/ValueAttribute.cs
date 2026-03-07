using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tebot.Attributes
{
    [System.AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public abstract class ValueAttribute : Attribute
    {
        public string Value { get; private set; }
        public ValueAttribute(string value)
        {
            Value = value;
        }
    }
}
