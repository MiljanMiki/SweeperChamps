using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using SC_Backend.DataModels;

namespace SC_Backend.DataContext;

public partial class ApplicationDbContext : DbContext
{
    /*
     * To protect potentially sensitive information in your connection string, you should move it out of source code. 
     * You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration 
     * - see https://go.microsoft.com/fwlink/?linkid=2131148.
     * For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
    
    Could not load database collations.
     */
    public ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Game> Games { get; set; }

    public virtual DbSet<GamePlayer> GamePlayers { get; set; }

    public virtual DbSet<GameSetting> GameSettings { get; set; }

    public virtual DbSet<Move> Moves { get; set; }

    public virtual DbSet<User> Users { get; set; }

//    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
//#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
//        => optionsBuilder.UseNpgsql(/*ConnectionString*/);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Game>(entity =>
        {
            entity.HasKey(e => e.GamesId).HasName("games_pkey");
        });

        modelBuilder.Entity<GamePlayer>(entity =>
        {
            entity.HasKey(e => e.GamePlayersId).HasName("game_players_pkey");

            entity.Property(e => e.Score).HasDefaultValue(0);

            entity.HasOne(d => d.Game).WithMany(p => p.GamePlayers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("gamePlayers_gameId_fkey");

            entity.HasOne(d => d.Player).WithMany(p => p.GamePlayers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("gamePlayers_playerId_fkey");
        });

        modelBuilder.Entity<GameSetting>(entity =>
        {
            entity.HasKey(e => e.GameSettingsId).HasName("game_settings_pkey");

            entity.Property(e => e.GameId).ValueGeneratedOnAdd();

            entity.HasOne(d => d.Game).WithMany(p => p.GameSettings)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("gameSettings_gameId_fkey");
        });

        modelBuilder.Entity<Move>(entity =>
        {
            entity.HasKey(e => e.MovesId).HasName("moves_pkey");

            entity.HasOne(d => d.Game).WithMany(p => p.Moves)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("moves_gameid_fkey");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UsersId).HasName("users_pkey");

            entity.Property(e => e.Elo).HasDefaultValue((short)0);
            entity.Property(e => e.UserRole).HasDefaultValueSql("'User'::character varying");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
