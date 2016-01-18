using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReiEventTest
{
    public class Snapshot
    {
        public Guid Id { get; private set; }
        public String RootId { get; private set; }
        public Int64 Version { get; private set; }
        public IDictionary<String, Object> State { get; private set; }

        public Snapshot() { }
        public Snapshot(Guid id, String rootId, Int64 version, IDictionary<String, Object> state)
        {
            Id = id; RootId = rootId; Version = version; State = state;
        }
    }
}
