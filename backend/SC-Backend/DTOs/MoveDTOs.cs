namespace SC_Backend.DTOs.Moves
{
    public record MoveDTO
    {
        public int GameId { get; set; }

        public string MoveLog { get; set; }
    }

    public record PutDTO
    {
        public string MoveLog { get; set; } = null!;
    }

}
