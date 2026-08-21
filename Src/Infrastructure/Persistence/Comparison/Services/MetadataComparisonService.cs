using Application.Abstractions.Comparison;
using Application.Features.Comparison.Commands;
using Application.Features.Comparison.DTOs;
using DataToolkit.Library;
using Microsoft.Extensions.Configuration;
using Persistence.Metadata.Services;
using Persistence.Migration.Metadata;
using System.Diagnostics;

namespace Persistence.Comparison.Services;

public sealed class MetadataComparisonService : IMetadataComparisonService
{
    private readonly MetadataService _metadataService;
    private readonly IConfiguration _configuration;

    public MetadataComparisonService(
        MetadataService metadataService,
        IConfiguration configuration)
    {
        _metadataService = metadataService;
        _configuration = configuration;
    }

    public async Task<MetadataComparisonResult> CompareAsync(
        CompareMetadataCommand command)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        Task<List<TableMetadata>> sourceTask =
            _metadataService.ExtractMetadataAsync(
                command.Source,
                command.Schema,
                command.Tables);

        Task<List<TableMetadata>> targetTask =
            _metadataService.ExtractMetadataAsync(
                command.Target,
                command.Schema,
                command.Tables);

        await Task.WhenAll(
            sourceTask,
            targetTask);

        List<TableMetadata> source =
            MetadataNormalizer.NormalizeColumns(
                sourceTask.Result);

        List<TableMetadata> target =
            MetadataNormalizer.NormalizeColumns(
                targetTask.Result);

        MetadataComparisonResult result =
            Compare(source, target);

        stopwatch.Stop();

        result.ElapsedMilliseconds =
            stopwatch.ElapsedMilliseconds;

        return result;
    }

    private static MetadataComparisonResult Compare(
        List<TableMetadata> source,
        List<TableMetadata> target)
    {
        Dictionary<string, TableMetadata> sourceLookup =
            source.ToDictionary(
                GetTableKey,
                StringComparer.OrdinalIgnoreCase);

        Dictionary<string, TableMetadata> targetLookup =
            target.ToDictionary(
                GetTableKey,
                StringComparer.OrdinalIgnoreCase);

        HashSet<string> tableKeys =
            sourceLookup.Keys
                .Concat(targetLookup.Keys)
                .ToHashSet(
                    StringComparer.OrdinalIgnoreCase);

        MetadataComparisonResult result = new();

        foreach (string tableKey in tableKeys.Order())
        {
            sourceLookup.TryGetValue(
                tableKey,
                out TableMetadata? sourceTable);

            targetLookup.TryGetValue(
                tableKey,
                out TableMetadata? targetTable);

            result.Summary.TablesCompared++;

            TableComparisonResult? comparison =
                CompareTable(
                    sourceTable,
                    targetTable);

            if (comparison is null)
                continue;

            result.Tables.Add(comparison);
        }

        BuildSummary(result);

        return result;
    }

    private static TableComparisonResult? CompareTable(
        TableMetadata? source,
        TableMetadata? target)
    {
        if (source is null && target is null)
            return null;

        TableMetadata table =
            source ?? target!;

        if (source is null)
        {
            return new TableComparisonResult
            {
                Table = GetTableKey(table),
                Differences = new Dictionary<string, List<string>>
                {
                    ["<TABLE>"] = ["Added"]
                }
            };
        }

        if (target is null)
        {
            return new TableComparisonResult
            {
                Table = GetTableKey(table),
                Differences = new Dictionary<string, List<string>>
                {
                    ["<TABLE>"] = ["Removed"]
                }
            };
        }

        Dictionary<string, List<string>> differences =
            CompareColumns(
                source,
                target);

        if (differences.Count == 0)
            return null;

        return new TableComparisonResult
        {
            Table = GetTableKey(source),
            Differences = differences
        };
    }

    private static Dictionary<string, List<string>> CompareColumns(
        TableMetadata source,
        TableMetadata target)
    {
        Dictionary<string, ColumnMetadata> sourceColumns =
            source.Columns.ToDictionary(
                c => c.Name,
                StringComparer.OrdinalIgnoreCase);

        Dictionary<string, ColumnMetadata> targetColumns =
            target.Columns.ToDictionary(
                c => c.Name,
                StringComparer.OrdinalIgnoreCase);

        HashSet<string> columnNames =
            sourceColumns.Keys
                .Concat(targetColumns.Keys)
                .ToHashSet(
                    StringComparer.OrdinalIgnoreCase);

        Dictionary<string, List<string>> differences =
            new(StringComparer.OrdinalIgnoreCase);

        foreach (string columnName in columnNames.Order())
        {
            sourceColumns.TryGetValue(
                columnName,
                out ColumnMetadata? sourceColumn);

            targetColumns.TryGetValue(
                columnName,
                out ColumnMetadata? targetColumn);

            if (sourceColumn is null)
            {
                differences[columnName] =
                    ["Added"];

                continue;
            }

            if (targetColumn is null)
            {
                differences[columnName] =
                    ["Removed"];

                continue;
            }

            List<string> columnDifferences =
                CompareColumnProperties(
                    sourceColumn,
                    targetColumn);

            if (columnDifferences.Count == 0)
                continue;

            differences[columnName] =
                columnDifferences;
        }

        return differences;
    }

    private static List<string> CompareColumnProperties(
        ColumnMetadata source,
        ColumnMetadata target)
    {
        List<string> differences = [];

        // Tipo de dato
        if (!Equals(source.SqlType, target.SqlType))
        {
            differences.Add("Type");
        }
        else
        {
            // Longitud solo se compara cuando el tipo es el mismo.
            if (!Equals(source.MaxLength, target.MaxLength))
            {
                differences.Add("Length");
            }

            if (!Equals(source.Precision, target.Precision))
            {
                differences.Add("Precision");
            }

            if (!Equals(source.Scale, target.Scale))
            {
                differences.Add("Scale");
            }
        }

        // NULL / NOT NULL
        if (source.IsNullable != target.IsNullable)
        {
            differences.Add("Nullable");
        }

        // IDENTITY
        if (source.IsIdentity != target.IsIdentity)
        {
            differences.Add("Identity");
        }

        // COMPUTED
        if (source.IsComputed != target.IsComputed)
        {
            differences.Add("Computed");
        }

        // Primary Key
        if (source.IsPrimaryKey != target.IsPrimaryKey)
        {
            differences.Add("PrimaryKey");
        }

        return differences;
    }

    private static void AddDifference(
        List<string> differences,
        string name,
        object? source,
        object? target)
    {
        if (Equals(source, target))
            return;

        if (source is null && target is null)
            return;

        differences.Add(name);
    }

    private static void BuildSummary(
        MetadataComparisonResult result)
    {
        result.Summary.TablesWithDifferences =
            result.Tables.Count;

        result.Summary.TablesOnlyInSource =
            result.Tables.Count(
                t =>
                    t.Differences.Count == 1 &&
                    t.Differences.ContainsKey("<TABLE>") &&
                    t.Differences["<TABLE>"].Contains("Removed"));

        result.Summary.TablesOnlyInTarget =
            result.Tables.Count(
                t =>
                    t.Differences.Count == 1 &&
                    t.Differences.ContainsKey("<TABLE>") &&
                    t.Differences["<TABLE>"].Contains("Added"));

        result.Summary.ColumnsAdded =
            result.Tables
                .Where(IsColumnTable)
                .SelectMany(
                    t => t.Differences.Values)
                .Count(
                    d => d.Contains("Added"));

        result.Summary.ColumnsRemoved =
            result.Tables
                .Where(IsColumnTable)
                .SelectMany(
                    t => t.Differences.Values)
                .Count(
                    d => d.Contains("Removed"));

        result.Summary.ColumnsChanged =
            result.Tables
                .Where(IsColumnTable)
                .SelectMany(
                    t => t.Differences.Values)
                .Count(
                    d =>
                        d.Count > 0 &&
                        !d.Contains("Added") &&
                        !d.Contains("Removed"));
    }

    private static bool IsColumnTable(
        TableComparisonResult table)
    {
        return
            !(
                table.Differences.Count == 1 &&
                table.Differences.ContainsKey("<TABLE>"));
    }

    private static string GetTableKey(
        TableMetadata table)
    {
        return $"{table.Schema}.{table.Name}";
    }
}