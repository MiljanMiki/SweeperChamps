namespace SC_GameServer.GameEngine;

public enum CellState
{
    Hidden,
    Revealed,
    Flagged
}

public class Cell
{
    public int X { get; }
    public int Y { get; }
    public bool IsMine { get; set; }
    public int AdjacentMineCount { get; set; }
    public CellState State { get; set; } = CellState.Hidden;

    public Cell(int x, int y)
    {
        X = x;
        Y = y;
    }
}

/// <summary>
/// Owns the grid, mine placement, and adjacency counts. Mines and adjacency
/// are computed once here at game creation, not recalculated per move.
/// </summary>
public class Board
{
    public int Width { get; }
    public int Height { get; }
    public int MineCount { get; }
    public Cell[,] Grid { get; }

    public int TotalCells => Width * Height;
    public int SafeCellCount => TotalCells - MineCount;

    private static readonly (int dx, int dy)[] Neighbors8 =
    {
        (-1, -1), (0, -1), (1, -1),
        (-1, 0),           (1, 0),
        (-1, 1),  (0, 1),  (1, 1)
    };

    public Board(int width, int height, int mineCount, int? randomSeed = null)
    {
        Width = width;
        Height = height;
        MineCount = mineCount;

        Grid = new Cell[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                Grid[x, y] = new Cell(x, y);

        PlaceMines(randomSeed);
        CalculateAdjacency();
    }

    private void PlaceMines(int? seed)
    {
        var rng = seed.HasValue ? new Random(seed.Value) : new Random();
        int placed = 0;
        while (placed < MineCount)
        {
            int x = rng.Next(Width);
            int y = rng.Next(Height);
            if (!Grid[x, y].IsMine)
            {
                Grid[x, y].IsMine = true;
                placed++;
            }
        }
    }

    private void CalculateAdjacency()
    {
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                if (!Grid[x, y].IsMine)
                    Grid[x, y].AdjacentMineCount = CountAdjacentMines(x, y);
    }

    private int CountAdjacentMines(int x, int y)
    {
        int count = 0;
        foreach (var (dx, dy) in Neighbors8)
        {
            int nx = x + dx, ny = y + dy;
            if (InBounds(nx, ny) && Grid[nx, ny].IsMine) count++;
        }
        return count;
    }

    public bool InBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

    public IEnumerable<Cell> GetNeighbors(int x, int y)
    {
        foreach (var (dx, dy) in Neighbors8)
        {
            int nx = x + dx, ny = y + dy;
            if (InBounds(nx, ny)) yield return Grid[nx, ny];
        }
    }

    /// <summary>
    /// Reveals (startX, startY). If it has zero adjacent mines, cascades
    /// through every connected zero-count cell, also revealing (but not
    /// cascading past) the non-zero cells bordering that region - standard
    /// Minesweeper flood fill. Mines are never auto-revealed by a cascade
    /// (a zero-count cell can't neighbor a mine by definition).
    /// Returns every cell newly revealed by this call, including the start cell.
    /// Caller must ensure the start cell is Hidden and not a mine.
    /// </summary>
    public List<Cell> RevealWithCascade(int startX, int startY)
    {
        var revealed = new List<Cell>();
        var start = Grid[startX, startY];

        start.State = CellState.Revealed;
        revealed.Add(start);

        var queue = new Queue<Cell>();
        if (start.AdjacentMineCount == 0) queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var neighbor in GetNeighbors(current.X, current.Y))
            {
                if (neighbor.State != CellState.Hidden || neighbor.IsMine) continue;

                neighbor.State = CellState.Revealed;
                revealed.Add(neighbor);

                if (neighbor.AdjacentMineCount == 0)
                    queue.Enqueue(neighbor); // keep cascading through more zero cells
                // non-zero neighbors are revealed but don't cascade further
            }
        }

        return revealed;
    }
}
