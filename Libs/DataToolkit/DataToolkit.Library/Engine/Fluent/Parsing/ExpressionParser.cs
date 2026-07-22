using System.Linq.Expressions;
using DataToolkit.Library.Fluent.Sql;

namespace DataToolkit.Library.Fluent.Parsing;

internal static class ExpressionParser
{
    public static SqlNode Parse(Expression expr)
    {
        return ParseInternal(expr);
    }

    private static SqlNode ParseInternal(Expression expr)
    {
        return expr switch
        {
            BinaryExpression b => ParseBinary(b),

            MemberExpression m => new SqlRaw($"[{m.Member.Name}]"),

            ConstantExpression c => new SqlParameter(
                $"@p{Guid.NewGuid():N}",
                c.Value
            ),

            UnaryExpression u => ParseInternal(u.Operand),

            _ => throw new NotSupportedException($"Expression not supported: {expr.NodeType}")
        };
    }

    private static SqlNode ParseBinary(BinaryExpression b)
    {
        // IMPORTANTE: normalizar primero ambos lados
        var left = ParseInternal(b.Left);
        var right = ParseInternal(b.Right);

        var op = GetOp(b.NodeType);

        // 🔥 AND / OR deben respetar asociatividad natural
        if (op is "AND" or "OR")
        {
            return BuildLogicalTree(left, op, right);
        }

        return new SqlBinary(left, op, right);
    }

    private static SqlNode BuildLogicalTree(SqlNode left, string op, SqlNode right)
    {
        /*
         * Esto evita árboles “en cadena plana”
         * y asegura estructura balanceada lógica
         */

        // Caso: A AND B AND C -> se encadena correctamente
        if (left is SqlBinary lb && lb.Op == op)
        {
            return new SqlBinary(
                lb.Left,
                op,
                new SqlBinary(lb.Right, op, right)
            );
        }

        return new SqlBinary(left, op, right);
    }

    private static string GetOp(ExpressionType type)
    {
        return type switch
        {
            ExpressionType.AndAlso => "AND",
            ExpressionType.OrElse => "OR",

            ExpressionType.Equal => "=",
            ExpressionType.NotEqual => "<>",
            ExpressionType.GreaterThan => ">",
            ExpressionType.LessThan => "<",
            ExpressionType.GreaterThanOrEqual => ">=",
            ExpressionType.LessThanOrEqual => "<=",

            _ => throw new NotSupportedException($"Operator not supported: {type}")
        };
    }
}