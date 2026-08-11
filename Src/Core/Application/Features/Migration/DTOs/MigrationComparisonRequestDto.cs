using Domain.Enums;
using System.ComponentModel;

namespace Application.Features.Migration.DTOs;

public sealed class MigrationComparisonRequestDto
{
    /// <summary>
    /// Proyecto sobre el que se generarán los artefactos.
    /// </summary>
    [DefaultValue("Master")]
    public string? ProjectName { get; set; }

    /// <summary>
    /// Conexión lógica de origen.
    /// </summary>
    [DefaultValue("Target")]
    public string Source { get; set; } = "Target";

    /// <summary>
    /// Conexión lógica de destino.
    /// </summary>
    [DefaultValue("Knowledge")]
    public string Target { get; set; } = "Knowledge";

    /// <summary>
    /// Esquema a comparar.
    /// Null = todos los esquemas.
    /// </summary>
    [DefaultValue("dbo")]
    public string? Schema { get; set; }

    /// <summary>
    /// Define el tipo (WorkFile = 0 o Homologation = 1).
    /// </summary>
    [DefaultValue(ArtifactType.WorkFile)]
    public ArtifactType ArtifactType { get; set; } = ArtifactType.WorkFile;

    /// <summary>
    /// Tablas específicas a comparar.
    /// Null o vacío = todas las tablas.
    /// </summary>
    [DefaultValue("[]")]
    public List<string> Tables { get; set; } = [];
}