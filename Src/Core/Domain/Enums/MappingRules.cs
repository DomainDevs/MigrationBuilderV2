namespace Domain.Enums;

/*
Semántica:
-------------------------------------------------------------------
Regla	Significado
DIRECT	Mismo nombre y tipo compatible
REVIEW	El Builder cree que corresponde pero necesita validación
UNMAPPED	No encontró correspondencia
LOOKUP	Requiere homologación/parametrización
CONVERT	Requiere conversión de tipo
DEFAULT	Se genera un valor fijo
*/
public static class MappingRules
{
    public const string Direct = "DIRECT";

    public const string Review = "REVIEW";

    public const string Unmapped = "UNMAPPED";

    public const string Lookup = "LOOKUP";

    public const string Convert = "CONVERT";

    public const string Default = "DEFAULT";
}
