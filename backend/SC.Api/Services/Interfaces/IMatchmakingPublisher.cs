using SC.Messaging.Matchmaker;

namespace SC.Api.Services.Interfaces
{
    public interface IMatchmakingPublisher
    {
        void PublishTicket(MatchTicketRequest ticket);
        void PublishCancelTicket(string userId);
    }
}
