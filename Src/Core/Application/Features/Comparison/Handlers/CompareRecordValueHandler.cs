using Application.Abstractions.Comparison;
using Application.Features.Comparison.Commands;
using Application.Features.Comparison.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Comparison.Handlers;

public sealed class CompareRecordValueHandler { 
    
    private readonly IRecordValueComparisonService _comparisonService; 
    public CompareRecordValueHandler(IRecordValueComparisonService comparisonService) {
        
        _comparisonService = comparisonService; 

    } 
    public async Task<RecordValueComparisonResult> HandleAsync(CompareRecordValueCommand command) { 
        
        return await _comparisonService.CompareAsync(command);

    } 
}
