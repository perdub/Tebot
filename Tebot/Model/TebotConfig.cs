using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tebot.Model
{
    public record TebotConfig
    {
        public String StorageName { get; set; } = "bot-data";
        public String StateName { get; set; } = "my-state";
    }
}
