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

    public virtual DbSet<UserStats> UserStats { get; set; }

//    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
//#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
//        => optionsBuilder.UseNpgsql(/*ConnectionString*/);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Game>(entity =>
        {
            entity.HasKey(e => e.GamesId).HasName("games_pkey");

            //entity.HasOne(g => g.GameSettings)
            //      .WithMany(gs => gs.Game)
            //      .HasConstraintName("game_gameSettingsId_fk");

            entity.Property(e => e.Status).HasConversion<string>();
        });

        modelBuilder.Entity<Game>(entity =>
            entity.ToTable("games", tb =>
            {
                tb.HasCheckConstraint("CK_valid_end_time", "end_time IS NULL OR (end_time > start_time)");
            })
        );

        modelBuilder.Entity<GamePlayer>(entity =>
        {
            entity.HasKey(e => e.GamePlayersId).HasName("game_players_pkey");

            entity.Property(e => e.TeamColor).HasConversion<string>();

            entity.Property(e => e.Score).HasDefaultValue(0);

            entity.HasOne(d => d.Game).WithMany(p => p.GamePlayers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("gamePlayers_gameId_fkey");

            entity.HasOne(d => d.Player).WithMany(p => p.GamePlayers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("gamePlayers_playerId_fkey");
        });

        modelBuilder.Entity<GamePlayer>(entity =>
            entity.ToTable("game_players", tb =>
            {
                tb.HasCheckConstraint("CK_game_players_score", "score >= 0");
                tb.HasCheckConstraint("CK_game_players_team_color", "team_color IN ('Red','Blue')");
            })
        );

        modelBuilder.Entity<GameSetting>(entity =>
        {
            entity.HasKey(e => e.GameSettingsId).HasName("game_settings_pkey");

            entity.Property(e => e.WinCondition).HasConversion<string>();

            //index za brzu pretragu partija
            entity.HasIndex(e => new
            {
                e.WinCondition,
                e.Width,
                e.Height,
                e.NumberOfMines
            }).HasDatabaseName("IX_game_settings_template_lookup");

        });

        modelBuilder.Entity<GameSetting>(entity =>
            entity.ToTable("game_settings", tb =>
            {
                tb.HasCheckConstraint("CK_game_settings_width", "width >= 9 AND width <= 50 ");
                tb.HasCheckConstraint("CK_game_settings_height", "height >= 9 AND height <= 50");
                tb.HasCheckConstraint("CK_game_settings_number_of_mines", "number_of_mines >= 10 AND number_of_mines <= 500");
                tb.HasCheckConstraint("CK_game_settings_start_time_seconds", "start_time_seconds IS NULL OR (start_time_seconds >= 30 AND start_time_seconds <= 1200)");//max 20 minuta
                tb.HasCheckConstraint("CK_game_settings_team_size", "team_size >= 1 AND team_size <= 3");
                tb.HasCheckConstraint("CK_game_settings_win_condition", "win_condition IN ('Race','TimeRush')");

                tb.HasCheckConstraint("CK_game_settings_mines_fit", "number_of_mines < (width * height)");
            })
        );


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
            entity.Property(e => e.UserRole).HasConversion<string>();
            entity.Property(e => e.UserRole).HasDefaultValueSql("'NotSet'::character varying");
            entity.Property(e => e.Datecreated).HasDefaultValueSql("CURRENT_DATE");
        });

        modelBuilder.Entity<User>(entity =>
            entity.ToTable("users",tb=>
            {
                tb.HasCheckConstraint("CK_users_email", "email LIKE '%@%'");
                //tb.HasCheckConstraint("CK_users_datecreated", "datecreated");
                tb.HasCheckConstraint("CK_users_elo", "elo >=0 AND elo <=  32767"); //32767 je max za smallint
                tb.HasCheckConstraint("CK_users_user_role", "user_role IN ('NotSet','User','Admin')");
            })
        );

       modelBuilder.Entity<UserStats>(entity =>
        {
            entity.HasKey(e => new { e.GameSettingId, e.UserId, e.IsRanked });

            entity.HasOne(us => us.User)
                    .WithMany(u => u.UserStats)
                    .HasForeignKey(us => us.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(us => us.GameSetting)
                    .WithMany() 
                    .HasForeignKey(us => us.GameSettingId)
                    .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.GamesPlayed).HasDefaultValue(0);
            entity.Property(e => e.Wins).HasDefaultValue(0);
            entity.Property(e => e.Losses).HasDefaultValue(0);
            entity.Property(e => e.PlayTime).HasDefaultValue((long)0);
        });

        //samo check constraints
        modelBuilder.Entity<UserStats>(entity =>
        {
            entity.ToTable("user_stats", tb =>
            {
                tb.HasCheckConstraint("CK_user_stats_games_played", "games_played >= 0");
                tb.HasCheckConstraint("CK_user_stats_wins", "wins >= 0");
                tb.HasCheckConstraint("CK_user_stats_losses", "losses >= 0");
                tb.HasCheckConstraint("CK_user_stats_playtime", "playtime >= 0");

                // Optional: Business logic check (wins + losses cannot exceed games played)
                tb.HasCheckConstraint("CK_user_stats_valid_outcomes", "(wins + losses) <= games_played");
            });
        });



        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
