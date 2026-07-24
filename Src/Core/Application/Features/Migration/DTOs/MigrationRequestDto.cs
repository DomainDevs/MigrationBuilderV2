using Domain.Enums;
using System.ComponentModel;

namespace Application.Features.Migration.DTOs;

public class MigrationRequestDto
{

    /// <summary> 
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
    public List<string> Tables { get; set; } = [];
}
