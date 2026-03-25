using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SC_Backend.DTOs
{
    public class LoginResponseDto
    {
        public string Token { get; set; }
        public DateTime Expires { get; set; }
        public UserInfoDto User { get; set; }
    }
    public class UserInfoDto
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public DateTime CreatedAt { get; set; }
        public int Elo { get; set; }
        public string SlikaUrl { get; set; }
    }
    public class RegisterDto
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
        public string Password{ get; set; }

        [Column("Role")]
        [MaxLength(20)]
        public string Role { get; set; } = "User";

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("Elo")]
        public int Elo { get; set; }
        public string SlikaURL { get; set; }
    }
    public class LoginDto
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }
    }

}
