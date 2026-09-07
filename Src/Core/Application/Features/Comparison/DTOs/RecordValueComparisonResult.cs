using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Comparison.DTOs;

public sealed class RecordValueComparisonResult
{
    public long ElapsedMilliseconds { get; set; }
    public RecordValueComparisonSummary Summary { get; set; } = new();
    public List<TableValueComparisonResult> Tables { get; set; } = [];
}