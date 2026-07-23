using System;
using System.Text.Json.Serialization;

namespace Application.Features.Execution.DTOs
{
    public class PackageExecutionQueryResponseDto
    {
        //[JsonPropertyName("executionId")]
        public int ExecutionId { get; set; }

        [JsonPropertyName("fileId")]
        public string FileId { get; set; } = "";

        [JsonPropertyName("fileName")]
        public string FileName { get; set; } = "";

        [JsonPropertyName("status")]
        public string Status { get; set; } = "";

        [JsonPropertyName("startTime")]
        public DateTime? StartTime { get; set; }

        [JsonPropertyName("endTime")]
        public DateTime? EndTime { get; set; }

        [JsonPropertyName("durationMs")]
        public long? DurationMs { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = "";

    }
}