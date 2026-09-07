using Application.Features.Comparison.Commands;
using Application.Features.Comparison.DTOs;

namespace Application.Abstractions.Comparison;


public interface IRecordValueComparisonService
{
    Task<RecordValueComparisonResult> CompareAsync(
        CompareRecordValueCommand command);
}