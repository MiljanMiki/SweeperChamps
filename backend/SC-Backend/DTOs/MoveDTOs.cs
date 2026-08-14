namespace SC_Backend.DTOs.Moves
{
    public record MoveDTO
    {
        public int GameId { get; set; }

        public string MoveLog { get; set; }
    }
}
