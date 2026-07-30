using DataToolkit.Library;

namespace Persistence.Planning.Services;

#region Modelos de Dominio del Planificador

public sealed class MigrationBatch
{
    public required int Stage { get; init; }
    public required IReadOnlyList<TableMetadata> Tables { get; init; }
    public bool Parallelizable => Tables.Count > 1;
}

public sealed class MigrationGraphStats
{
    public required int TotalTables { get; init; }
    public required int TotalDependencies { get; init; }
    public required int TotalBatches { get; init; }
    public required int MaxDepth { get; init; }
    public required int RootTablesCount { get; init; }
    public required int LeafTablesCount { get; init; }
    public required int SelfReferencedTablesCount { get; init; }
}

public sealed class MigrationPlanResult
{
    public required IReadOnlyList<MigrationBatch> Batches { get; init; }
    public required IReadOnlyList<TableMetadata> FlatOrderedTables { get; init; }
    public required MigrationGraphStats Stats { get; init; }
    public required IReadOnlyList<TableMetadata> SelfReferencedTables { get; init; }
}

#endregion

public sealed class DependencyAnalyzer
{
    /// <summary>
    /// Punto de entrada principal: Analiza el grafo en un solo pase
    /// y genera el plan completo con lotes, orden plano y estadísticas.
    /// </summary>
    public MigrationPlanResult Analyze(IReadOnlyCollection<TableMetadata> metadata)
    {
        if (metadata is null || metadata.Count == 0)
        {
            return new MigrationPlanResult
            {
                Batches = [],
                FlatOrderedTables = [],
                SelfReferencedTables = [],
                Stats = new MigrationGraphStats
                {
                    TotalTables = 0,
                    TotalDependencies = 0,
                    TotalBatches = 0,
                    MaxDepth = 0,
                    RootTablesCount = 0,
                    LeafTablesCount = 0,
                    SelfReferencedTablesCount = 0
                }
            };
        }

        // 1. Construir el grafo marcando SelfReferences y recopilando métricas iniciales
        var (nodes, totalDependencies) = BuildDependencyGraph(metadata);

        // PriorityQueue para garantizar un orden determinista en la cola de Kahn (por nombre de tabla)
        var priorityQueue = new PriorityQueue<Node, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var node in nodes.Values.Where(x => x.InDegree == 0))
        {
            priorityQueue.Enqueue(node, node.Table.Name);
        }

        var batches = new List<MigrationBatch>();
        var flatOrdered = new List<TableMetadata>(nodes.Count);
        int currentStage = 0;

        // 2. Ejecutar algoritmo de Kahn por Stages (Niveles)
        while (priorityQueue.Count > 0)
        {
            var stageNodes = new List<Node>();

            // Extraer todos los nodos independientes del stage actual
            while (priorityQueue.Count > 0)
            {
                stageNodes.Add(priorityQueue.Dequeue());
            }

            var stageTables = new List<TableMetadata>(stageNodes.Count);
            var nextStageQueue = new PriorityQueue<Node, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var node in stageNodes)
            {
                node.Stage = currentStage;
                stageTables.Add(node.Table);
                flatOrdered.Add(node.Table);

                foreach (var child in node.Children)
                {
                    child.InDegree--;
                    if (child.InDegree == 0)
                    {
                        nextStageQueue.Enqueue(child, child.Table.Name);
                    }
                }
            }

            batches.Add(new MigrationBatch
            {
                Stage = currentStage,
                Tables = stageTables
            });

            priorityQueue = nextStageQueue;
            currentStage++;
        }

        // 3. Validar Ciclos
        if (flatOrdered.Count != nodes.Count)
        {
            var circularTables = nodes.Values
                .Where(n => n.InDegree > 0)
                .Select(n => $"{n.Table.Schema}.{n.Table.Name}");

            throw new InvalidOperationException(
                $"Dependencias circulares detectadas en las siguientes tablas: {string.Join(", ", circularTables)}");
        }

        // 4. Calcular Estadísticas Gratis
        var selfRefTables = nodes.Values.Where(n => n.HasSelfReference).Select(n => n.Table).ToList();
        var rootTablesCount = nodes.Values.Count(n => n.Parents.Count == 0);
        var leafTablesCount = nodes.Values.Count(n => n.Children.Count == 0);

        var stats = new MigrationGraphStats
        {
            TotalTables = nodes.Count,
            TotalDependencies = totalDependencies,
            TotalBatches = batches.Count,
            MaxDepth = batches.Count,
            RootTablesCount = rootTablesCount,
            LeafTablesCount = leafTablesCount,
            SelfReferencedTablesCount = selfRefTables.Count
        };

        return new MigrationPlanResult
        {
            Batches = batches,
            FlatOrderedTables = flatOrdered,
            SelfReferencedTables = selfRefTables,
            Stats = stats
        };
    }

    #region Compatibilidad con firmas anteriores (Delegación directa)

    public IReadOnlyList<TableMetadata> Sort(IReadOnlyCollection<TableMetadata> metadata)
        => Analyze(metadata).FlatOrderedTables;

    public IReadOnlyList<string> SortToString(IReadOnlyCollection<TableMetadata> metadata)
        => Analyze(metadata).FlatOrderedTables.Select(t => t.Name).ToList();

    #endregion

    #region Helpers Internos

    private static (Dictionary<string, Node> Nodes, int TotalDependencies) BuildDependencyGraph(
        IReadOnlyCollection<TableMetadata> metadata)
    {
        var nodes = metadata.ToDictionary(
            x => $"{x.Schema}.{x.Name}",
            x => new Node(x),
            StringComparer.OrdinalIgnoreCase);

        int totalDependencies = 0;

        foreach (var node in nodes.Values)
        {
            foreach (var column in node.Table.Columns)
            {
                if (string.IsNullOrWhiteSpace(column.ForeignTable))
                    continue;

                // Caso 1: Detectar y registrar Self-Reference explícitamente
                if (string.Equals(node.Table.Name, column.ForeignTable, StringComparison.OrdinalIgnoreCase))
                {
                    node.HasSelfReference = true;
                    continue;
                }

                var key = $"{node.Table.Schema}.{column.ForeignTable}";

                if (!nodes.TryGetValue(key, out var parent))
                    continue;

                if (node.Parents.Contains(parent))
                    continue;

                node.Parents.Add(parent);
                parent.Children.Add(node);
                totalDependencies++;
            }
        }

        foreach (var node in nodes.Values)
        {
            node.InDegree = node.Parents.Count;
        }

        return (nodes, totalDependencies);
    }

    private sealed class Node
    {
        public Node(TableMetadata table)
        {
            Table = table;
        }

        public TableMetadata Table { get; }
        public List<Node> Parents { get; } = [];
        public List<Node> Children { get; } = [];
        public int InDegree;
        public int Stage;
        public bool HasSelfReference;
    }

    #endregion

    public static KeyValuePair<string, string>[] ConfigureServices() =>
    [
        new("Lifetime", "Singleton")
    ];
}