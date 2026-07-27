using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Migration.DTOs;

public class MigrationResponseDto
{
    public int GeneratedFiles { get; init; }

    public int SkippedTables { get; init; }

    public List<string> Files { get; init; } = [];

    public List<string> Warnings { get; init; } = [];
}
