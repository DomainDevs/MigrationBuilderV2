namespace DataToolkit.Bootstrap.Diagnostics;

internal static class TypeDisplay
{
    internal static string GetName(Type type)
    {
        string typeName = type.Name;

        if (!type.IsGenericType)
        {
            return typeName;
        }

        int genericMarker = typeName.IndexOf('`');

        // Se muestra únicamente la aridad del tipo genérico (<2>, <3>, etc.)
        return string.Concat(
            typeName.AsSpan(0, genericMarker),
            "<",
            typeName.AsSpan(genericMarker + 1),
            ">");

        /*
        Type[] arguments = type.GetGenericArguments();
        string[] names = new string[arguments.Length];

        for (int i = 0; i < arguments.Length; i++)
        {
            names[i] = GetName(arguments[i]);
        }

        return string.Concat(
            typeName.AsSpan(0, genericMarker),
            "<",
            string.Join(", ", names),
            ">");
        */
    }
}