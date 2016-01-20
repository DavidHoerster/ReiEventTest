using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReiEventTest.Entity
{
    public class Answer
    {
        public String ControlId { get; private set; }
        public Object[] Values { get; private set; }

        public Answer(String id, Object[] vals)
        {
            ControlId = id; Values = vals;
        }
    }

    public class REI
    {
        public String Id { get; private set; }
        public Guid FormId { get; private set; }
        public IEnumerable<Answer> Answers { get; private set; }

        public REI(String id, Guid formId, IEnumerable<Answer> answers)
        {
            Id = id; FormId = formId; Answers = answers;
        }
    }
}