using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Comparison.DTOs;

public sealed class ColumnValueComparisonResult
{
    public string Column { get; set; } = "";
    public string SqlType { get; set; } = "";
    public decimal SourceValue { get; set; }
    public decimal TargetValue { get; set; }
    public decimal Difference { get; set; }
    public string Status { get; set; } = "";
}