using DataToolkit.Library;

namespace Persistence.Planning.Services;

public sealed class DependencyAnalyzer
{
    //Complejo con data
    #region Sort
    public IReadOnlyList<TableMetadata> Sort(
        IReadOnlyCollection<TableMetadata> metadata)
    {
        var nodes = metadata.ToDictionary(
            x => $"{x.Schema}.{x.Name}",
            x => new Node(x),
            StringComparer.OrdinalIgnoreCase);

        foreach (var node in nodes.Values)
        {
            foreach (var column in node.Table.Columns)
            {
                if (string.IsNullOrWhiteSpace(column.ForeignTable))
                    continue;

                var key = $"{node.Table.Schema}.{column.ForeignTable}";

                if (!nodes.TryGetValue(key, out var parent))
                    continue;

                if (node.Parents.Contains(parent))
                    continue;

                node.Parents.Add(parent);
                parent.Children.Add(node);
            }
        }

        foreach (var node in nodes.Values)
            node.InDegree = node.Parents.Count;

        Queue<Node> queue = new(
            nodes.Values.Where(x => x.InDegree == 0));

        List<TableMetadata> ordered = new();

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            ordered.Add(current.Table);

            foreach (var child in current.Children)
            {
                child.InDegree--;

                if (child.InDegree == 0)
                    queue.Enqueue(child);
            }
        }

        if (ordered.Count != nodes.Count)
            throw new InvalidOperationException(
                "Existen dependencias circulares.");

        return ordered;
    }
    #endregion

    //metodo simple sin data
    #region SortToString
    public IReadOnlyList<string> SortToString(
        IReadOnlyCollection<TableMetadata> metadata)
    {
        var nodes = metadata.ToDictionary(
            x => $"{x.Schema}.{x.Name}",
            x => new Node(x),
            StringComparer.OrdinalIgnoreCase);

        foreach (var node in nodes.Values)
        {
            foreach (var column in node.Table.Columns)
            {
                if (string.IsNullOrWhiteSpace(column.ForeignTable))
                    continue;

                var key = $"{node.Table.Schema}.{column.ForeignTable}";

                if (!nodes.TryGetValue(key, out var parent))
                    continue;

                if (node.Parents.Contains(parent))
                    continue;

                node.Parents.Add(parent);
                parent.Children.Add(node);
            }
        }

        foreach (var node in nodes.Values)
        {
            node.InDegree = node.Parents.Count;
        }

        Queue<Node> queue = new(
            nodes.Values.Where(x => x.InDegree == 0));

        List<string> ordered = new();

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            ordered.Add(current.Table.Name);

            foreach (var child in current.Children)
            {
                child.InDegree--;

                if (child.InDegree == 0)
                {
                    queue.Enqueue(child);
                }
            }
        }

        if (ordered.Count != nodes.Count)
        {
            //throw new InvalidOperationException("Existen dependencias circulares.");
        }

        return ordered;
    }
    #endregion

    //Node, representa un nodo en el grafo de dependencias de tablas
    #region Node
    private sealed class Node
    {
        public Node(TableMetadata table)
        {
            Table = table;
        }

        public TableMetadata Table { get; }

        public List<Node> Parents { get; } = new();

        public List<Node> Children { get; } = new();

        public int InDegree;
    }
    #endregion

    //Define the service lifetime for DependencyAnalyzer 
    public static KeyValuePair<string, string>[] ConfigureServices() =>
    [
        new("Lifetime", "Singleton")
    ];

}