using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReiEventTest.Events
{
    public class RuleEvaluated : ControlFieldEventBase
    {
        public String RuleName { get; set; }
        public String Result { get; set; }
    }
}
