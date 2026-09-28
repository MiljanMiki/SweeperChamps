using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SC.Messaging.Matchmaker
{
    public class MatchTicketRequest
    {
        public string UserId { get; set; } = string.Empty;
        public int GameSettingsId { get; set; }
        public bool IsRanked { get; set; }
        public int RequiredPlayers { get; set; }
        public int? Elo { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class CancelTicketEvent
    {
        public string UserId { get; set; } = string.Empty;

        public CancelTicketEvent() { }

        public CancelTicketEvent(string userId)
        {
            UserId = userId;
        }
    }

    public class MatchFoundEvent
    {
        public int GameId { get; set; }
        public List<string> UserIds { get; set; } = new();
    }
}
