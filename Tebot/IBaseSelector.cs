using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tebot
{
    public interface IBaseSelector
    {
        Type SelectType(long id);
    }
}
