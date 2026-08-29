namespace Domain.Enums;

public enum MigrationParameterType
{
    // Texto
    String,
    AnsiString,
    Char,
    AnsiChar,

    // Enteros
    Byte,
    Int16,
    Int32,
    Int64,

    // Decimales / numéricos
    Decimal,
    Double,
    Single,

    // Fecha y hora
    DateTime,
    DateTime2,
    DateTimeOffset,
    Date,
    Time,

    // Boolean
    Boolean,

    // Identificadores
    Guid,

    // Binarios
    Binary,
    VarBinary,

    // SQL Server específicos
    SqlVariant,
    Xml,

    // Tipos espaciales de SQL Server
    Geometry,
    Geography,

    // Texto Unicode explícito
    NChar,
    NVarChar,

    // Texto largo
    Text,
    NText,

    // Binario largo
    Image,

    // Monetarios
    Money,
    SmallMoney
}

