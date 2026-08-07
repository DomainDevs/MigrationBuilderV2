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

    private static void ValidateDoubleQuotedStrings(string script)
    {
        bool inString = false;
        bool inLineComment = false;
        bool inBlockComment = false;

        for (int i = 0; i < script.Length; i++)
        {
            char c = script[i];
            char next = i + 1 < script.Length
                ? script[i + 1]
                : '\0';

            // -----------------------------------------
            // Comentario de línea
            // -----------------------------------------
            if (!inString && !inBlockComment)
            {
                if (!inLineComment && c == '-' && next == '-')
                {
                    inLineComment = true;
                    i++;
                    continue;
                }

                if (inLineComment)
                {
                    if (c == '\r' || c == '\n')
                        inLineComment = false;

                    continue;
                }
            }

            // -----------------------------------------
            // Comentario de bloque
            // -----------------------------------------
            if (!inString && !inLineComment)
            {
                if (!inBlockComment && c == '/' && next == '*')
                {
                    inBlockComment = true;
                    i++;
                    continue;
                }

                if (inBlockComment)
                {
                    if (c == '*' && next == '/')
                    {
                        inBlockComment = false;
                        i++;
                    }

                    continue;
                }
            }

            // -----------------------------------------
            // Literales de texto
            // -----------------------------------------
            if (!inLineComment && !inBlockComment)
            {
                if (c == '\'')
                {
                    // Manejar '' (comilla escapada)
                    if (inString && next == '\'')
                    {
                        i++;
                        continue;
                    }

                    inString = !inString;
                    continue;
                }

                // Solo detectar " fuera de cadenas y comentarios
                if (!inString && c == '"')
                {
                    int end = script.IndexOf('"', i + 1);

                    string value = end > i
                        ? script.Substring(i, end - i + 1)
                        : "\"";

                    throw new InvalidOperationException(
    $"""
Se detectó un posible literal de texto entre comillas dobles.

Valor encontrado:
    {value}

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
        }
    }

}