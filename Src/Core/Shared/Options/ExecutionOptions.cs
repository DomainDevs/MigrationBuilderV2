using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Options;

public sealed class ExecutionOptions
{
    public int BatchSize { get; init; } = 5000;
    public int BulkCopyTimeout { get; init; } = 300;
}
