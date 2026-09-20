using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SC.Messaging.Matchmaker
{
    public record MatchTicketRequest
    {
        public string UserId;

        public int GameSettingsId;

        public bool IsRanked;

        public int RequiredPlayers;

        public short? Elo;

        public DateTime Timestamp;
    }

    public record CancelTicketEvent(string UserId);

    public record MatchFoundEvent
    {
        public int GameId;
        public List<string> UserIds;
    }
}
