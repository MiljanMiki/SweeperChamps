using SC.Messaging.Matchmaker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SC.Matchmaker.Services.Interfaces
{
    public interface IMatchFoundPublisher
    {
        void Publish(MatchFoundEvent matchEvent);
    }
}
