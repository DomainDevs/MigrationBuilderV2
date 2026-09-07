using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Comparison.Commands;

public sealed class CompareRecordValueCommand {
    public required string Source { get; set; }
    public required string Target { get; set; }
    public string? Schema { get; set; }
    public List<string>? Tables { get; set; }
    public string? ProjectName { get; set; } 
}


