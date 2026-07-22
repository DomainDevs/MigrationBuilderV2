using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain;

/// <summary>
/// Clase base para todas las entidades del dominio.
/// Contiene auditoría y clave primaria.
/// </summary>
public abstract class BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // autoincrement
    public int Id { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("created_by")]
    public string? CreatedBy { get; set; }

    [Column("updated_by")]
    public string? UpdatedBy { get; set; }
}