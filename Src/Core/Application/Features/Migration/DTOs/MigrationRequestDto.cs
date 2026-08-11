using Domain.Enums;
using System.ComponentModel;

namespace Application.Features.Migration.DTOs;

public class MigrationRequestDto
{
    /// <summary>
    /// Proyecto sobre el que se generarán los artefactos.
    /// </summary>
    [DefaultValue("Master")]
    public string? ProjectName { get; set; }

    /// Esquema a comparar. 
    /// Null = todos los esquemas. 
    /// </summary>
    [DefaultValue("dbo")]
    public string? Schema { get; set; }

    /// <summary> 
    /// Define el tipo (WorkFile = 0 o Homologation = 1)
    /// Null = todos los esquemas. 
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
