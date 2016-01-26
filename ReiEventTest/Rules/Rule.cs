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
        public String Then { get; private set; }
        public String Else { get; private set; }
    }
}
