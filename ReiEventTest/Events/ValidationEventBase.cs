using Cti.Platform.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReiEventTest.Events
{
    public abstract class ValidationEventBase : ControlFieldEventBase
    {
        public String Validator { get; set; }
        public String Message { get; set; }
    }
}
