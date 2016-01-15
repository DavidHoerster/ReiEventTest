using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReiEventTest.Events
{
    public abstract class ControlFieldEventBase : ReiEventBase
    {
        public String ControlId { get; set; }
        public Object[] Values { get; set; }
    }
}
