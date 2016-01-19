using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReiEventTest.Entity
{
    public class Answer
    {
        public Guid ControlId { get; private set; }
        public String Path { get; private set; }
        public Object[] Values { get; private set; }
    }

    public class REI
    {
        public Guid Id { get; private set; }
        public Guid FormInstanceId { get; private set; }
        public Guid ReportingEntityId { get; private set; }
        public Guid ReportingEntityTypeId { get; private set; }
        public String ReportingEntityDisplayName { get; private set; }
        public IEnumerable<Answer> Answers { get; private set; }
    }
}