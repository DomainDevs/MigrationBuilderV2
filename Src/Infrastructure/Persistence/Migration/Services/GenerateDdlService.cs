using Abstractions.Migration.DDL;
using Application.Features.Migration.Commands;
using DataToolkit.Library;
using DataToolkit.Library.UnitOfWorkLayer;
using Persistence.Connect.Context;
using Persistence.Metadata.Services;

namespace Persistence.Migration.Services;

public sealed class GenerateDdlService : IGenerateDdlService
{
    private readonly IUnitOfWork _source; //context db1
    private readonly IUnitOfWork _target; //context db2
    private readonly MetadataService _metadataService;

    public GenerateDdlService(SqlServerContext context, MetadataService metadataService)
    {
        _source = context.Source;
        _target = context.Target;
        _metadataService = metadataService;
    }

    public async Task<string> GenerateDdlScriptsAsync(
        GenerateDdlCommand generateDdlCommand
    )
    {
        List<string> generatedFiles = [];

        List<TableMetadata> metadataSource = _metadataService.ExtractMetadataAsync(
            true,
            generateDdlCommand.Schema,
            generateDdlCommand.Tables).Result;

        List<TableMetadata> metadataTarget = _metadataService.ExtractMetadataAsync(
            false,
            generateDdlCommand.Schema,
            generateDdlCommand.Tables).Result;

        // Aquí puedes continuar con la lógica de tu método.
        var outputPath = "";
        outputPath = "D://"; //_workFileService.pathconfigure();
        //IOptions<MigrationOptions> options

        return "Generado!!";
    }

}
