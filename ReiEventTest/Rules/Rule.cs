using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReiEventTest.Rules
{
    public class Rule
    {
        public String Name { get; private set; }
        public String If { get; private set; }
        public RuleOutcome Then { get; private set; }
        public RuleOutcome Else { get; private set; }
    }
}
