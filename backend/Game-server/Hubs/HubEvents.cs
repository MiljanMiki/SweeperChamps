namespace SC_GameServer.Hubs;

public static class HubEvents
{
    public const string MoveMade        = "MoveMade";
    public const string MoveRejected    = "MoveRejected";
    public const string GameOver        = "GameOver";
    public const string PlayerConnected = "PlayerConnected";
    public const string TurnChanged     = "TurnChanged";
    public const string PlayerTimeout   = "PlayerTimeout";
}
