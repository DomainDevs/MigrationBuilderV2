using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Authentication;

[Table("tuser")]
public class User
{
    // id_user
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id_user")]
    public int IdUser { get; set; }

    // user_name
    [Required]
    [Column("user_name")]
    [MaxLength(100)]
    public string UserName { get; set; }

    // password_hash
    [Required]
    [Column("password_hash")]
    [MaxLength(256)]
    public string PasswordHash { get; set; }

    // enabled
    [Required]
    [Column("enabled")]
    public bool Enabled { get; set; }

    // created_at
    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    // updated_at
    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}