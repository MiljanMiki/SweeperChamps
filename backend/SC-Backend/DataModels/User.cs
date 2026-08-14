using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SC_Backend.DataModels;

[Table("users")]
[Index("Email", Name = "users_email_key", IsUnique = true)]
[Index("Username", Name = "users_username_key", IsUnique = true)]
public partial class User
{
    [Column("username")]
    [StringLength(50)]
    public string Username { get; set; } = null!;

    [Column("email")]
    [StringLength(60)]
    public string Email { get; set; } = null!;

    [Column("password_hash")]
    [MinLength(59)]
    [MaxLength(60)]
    public string PasswordHash { get; set; } = null!;

    [Column("datecreated")]
    public DateOnly Datecreated { get; set; }

    [Column("elo")]
    public short? Elo { get; set; }

    [Column("user_role")]
    [StringLength(20)]
    public UserRoles UserRole { get; set; }

    [Key]
    [Column("users_id")]
    public int UsersId { get; set; }

    [InverseProperty("Player")]
    public virtual ICollection<GamePlayer> GamePlayers { get; set; } = new List<GamePlayer>();

    [InverseProperty("User")]
    public virtual ICollection<UserStats> UserStats { get; set; } = new List<UserStats>();
}

public enum UserRoles
{
    NotSet = 0,
    User = 10,
    Admin = 20,
}

