using Microsoft.SqlServer.TransactSql.ScriptDom;
using System.Text.RegularExpressions;

namespace Persistence.Execution.Validation;

public static class SqlScriptValidator
{
    public static void Validate(
        string script)
    {
        ArgumentNullException.ThrowIfNull(script);

        ValidateSyntax(script);
        ValidateDoubleQuotedStrings(script);
    }

    private static void ValidateSyntax(
        string script)
    {
        var parser = new TSql170Parser(false);

        using var reader = new StringReader(script);

        TSqlFragment fragment =
            parser.Parse(
                reader,
                out IList<ParseError> errors);

        if (errors.Count == 0)
            return;

        var message =
            string.Join(
                Environment.NewLine,
                errors.Select(e =>
                    $"Línea {e.Line}, Columna {e.Column}: {e.Message}"));

        throw new InvalidOperationException(
$"""
El script SQL contiene errores de sintaxis y no puede ejecutarse.

{message}

Revise el archivo SQL y vuelva a ejecutar la migración.
""");
    }

    private static void ValidateDoubleQuotedStrings(
        string script)
    {
        var matches = Regex.Matches(
            script,
            "\"[^\"]+\"");

        if (matches.Count == 0)
            return;

        Match match = matches[0];

        throw new InvalidOperationException(
$"""
Se detectó un posible literal de texto entre comillas dobles.

Valor encontrado:
    {match.Value}

SQL Server utiliza comillas simples para representar
literales de texto.

Incorrecto:
    "PRUEBAS"

Correcto:
    'PRUEBAS'

Revise el archivo SQL y vuelva a ejecutar la migración.
""");
    }
}