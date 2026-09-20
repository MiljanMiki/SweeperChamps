using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SC.Messaging
{
    public static class RabbitMqConstants
    {
        #region matchmaking
        // Exchanges
        public const string MatchmakingExchange = "matchmaking.topic";

        // Queues
        public const string MatchmakingResultsQueue = "matchmaking.results.queue";
        public const string TicketCancellationsQueue = "matchmaking.cancellations.queue";

        // Routing Key Formats
        // e.g., "ticket.5.ranked" or "ticket.2.casual"
        public static string GetTicketRoutingKey(int gameSettingsId, bool isRanked)
            => $"ticket.{gameSettingsId}.{(isRanked ? "ranked" : "casual")}";

        #endregion matchmaking
    }
}
