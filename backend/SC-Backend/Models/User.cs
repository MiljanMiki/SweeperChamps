using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SC_Backend.Models
{
    public class User
    {
        [Key]
        [Column("ID")]
        public int ID { get; set; }

        [Required]
        [Column("Username")]
        [MaxLength(50)]
        public string Username { get; set; }

        [Required]
        [Column("Email")]
        [MaxLength(100)]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [Column("PasswordHash")]
        [MaxLength(255)]
        public string PasswordHash { get; set; }

        [Column("Role")]
        [MaxLength(20)]
        public string Role { get; set; } = "User";

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("Elo")]
        public int Elo { get; set; }
        public string SlikaURL { get; set; }
    }
    public static class UserRoles
    {
        public const string Admin = "Admin";
        public const string User = "User";
    }
}
