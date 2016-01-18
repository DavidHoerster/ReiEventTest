using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReiEventTest
{
    public interface ICanSnapshot
    {
        Snapshot TakeSnapshot();
        void LoadSnapshot(Snapshot snap);
    }
}
