using Application.Abstractions.Comparison;
using Application.Features.Comparison.Commands;
using Application.Features.Comparison.DTOs;

namespace Application.Features.Comparison.Handlers;

public sealed class CompareRecordCountHandler
{
    private readonly IRecordCountService _recordCountService;

    public CompareRecordCountHandler(
        IRecordCountService recordCountService)
    {
        _recordCountService = recordCountService;
    }

    public async Task<List<RecordCountResultDto>> HandleAsync(
        CompareRecordCountCommand command)
    {
        return await _recordCountService.CompareAsync(command);
    }
}