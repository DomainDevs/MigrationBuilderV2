using Application.Features.Comparison.Commands;
using Application.Features.Comparison.DTOs;

namespace Application.Abstractions.Comparison;

public interface IRecordCountService
{
    Task<List<RecordCountResultDto>> CompareAsync(CompareRecordCountCommand command);
}
