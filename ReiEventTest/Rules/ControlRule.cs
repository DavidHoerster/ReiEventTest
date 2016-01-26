using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReiEventTest.Rules
{
    public class ControlRule
    {
        public String Id { get; private set; }
        public Guid FormId { get; private set; }
        public String ControlId { get; private set; }
        public Rule Rule { get; private set; }
    }
}
