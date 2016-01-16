using Cti.Platform.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReiEventTest.Events
{
    public abstract class ReiEventBase : EventBase
    {
        public Guid FormId { get; set; }
        public String ReportingEntityInstanceId { get; set; }
        public Int64 Version { get; set; }
        public DateTime Date { get; set; }
    }
}