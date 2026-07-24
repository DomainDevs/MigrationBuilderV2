using Application.Features.Migration.Commands;

namespace Abstractions.Migration.DDL
{
    public interface IGenerateDdlService
    {
        Task<string> GenerateDdlScriptsAsync(GenerateDdlCommand generateDdlCommand);
    }
}