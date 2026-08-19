using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Comparison.DTOs;

public sealed class RecordCountResultDto
{
    public string Schema { get; init; } = string.Empty;
    public string Table { get; init; } = string.Empty;

    public long? SourceCount { get; init; }
    public long? TargetCount { get; init; }

    public string Status { get; init; } = string.Empty;
}