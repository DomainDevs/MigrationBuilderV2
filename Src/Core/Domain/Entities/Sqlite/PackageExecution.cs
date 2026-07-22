using Domain.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Sqlite;

[Table("PackageExecution")]
public sealed class PackageExecution
{
    /// <summary>
    /// Identificador único de la ejecución.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ExecutionId { get; set; }

    /// <summary>
    /// Identificador del archivo (0001, 0002...).
    /// </summary>
    [Required]
    [MaxLength(10)]
    public string FileId { get; set; } = string.Empty;

    /// <summary>
    /// Nombre del paquete o archivo.
    /// </summary>
    [Required]
    [MaxLength(260)]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Estado de la ejecución.
    /// Pending | Running | Completed | Failed | Skipped
    /// </summary>
    [Required]
    [MaxLength(20)]
    //public string Status { get; set; } = string.Empty;
    public PackageExecutionStatus Status { get; set; }

    /// <summary>
    /// Fecha y hora de inicio.
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// Fecha y hora de finalización.
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Duración de la ejecución en milisegundos.
    /// </summary>
    public long? DurationMs { get; set; }

    /// <summary>
    /// Mensaje informativo o de error.
    /// </summary>
    [MaxLength(4000)]
    public string? Message { get; set; }
}