using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Comparison.DTOs;

public sealed class RecordValueComparisonSummary
{
    public int TablesCompared { get; set; }
    public int TablesMatching { get; set; }
    public int TablesWithDifferences { get; set; }
    public int TablesOnlyInSource { get; set; }
    public int TablesOnlyInTarget { get; set; }
    public int TablesWithoutValueColumns { get; set; }
    public int ValueColumnsCompared { get; set; }
    public int ValueColumnsWithDifferences { get; set; }
}
