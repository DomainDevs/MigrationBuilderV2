using System.ComponentModel;

namespace Application.Features.Comparison.DTOs;

public sealed class CompareValidationRequestDto
{
    /// <summary>
    /// Conexión lógica de origen.
    /// </summary>
    [DefaultValue("Target")]
    public string Source { get; set; } = "Source";

    /// <summary>
    /// Conexión lógica de destino.
    /// </summary>
    [DefaultValue("Knowledge")]
    public string Target { get; set; } = "Target";

    /// <summary>
    /// Esquema a comparar.
    /// Null = todos los esquemas.
    /// </summary>
    [DefaultValue("dbo")]
    public string? Schema { get; set; }

    /// <summary>
    /// Tablas específicas a comparar.
    /// Null o vacío = todas las tablas.
    /// </summary>
    [DefaultValue("[]")]
    public List<string> Tables { get; set; } = [];
}