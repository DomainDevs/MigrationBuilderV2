using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Comparison.DTOs;

public sealed class TableValueComparisonResult
{
    public string Table { get; set; } = "";
    public string Status { get; set; } = "";
    public List<ColumnValueComparisonResult> Columns { get; set; } = [];
}
