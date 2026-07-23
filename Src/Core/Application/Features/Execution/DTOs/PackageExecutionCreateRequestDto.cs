using System;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace Application.Features.Execution.DTOs
{
    public class PackageExecutionCreateRequestDto
    {
        [JsonIgnore]
        //[JsonPropertyName("executionId")]
        //[Required]
        public int ExecutionId { get; set; }

        [JsonPropertyName("fileId")]
        [Required]
        [StringLength(20)]
        public string FileId { get; set; } = "";

        [JsonPropertyName("fileName")]
        [Required]
        [StringLength(520)]
        public string FileName { get; set; } = "";

        [JsonPropertyName("status")]
        [Required]
        [StringLength(40)]
        public string Status { get; set; } = "";

        [JsonPropertyName("startTime")]
        public DateTime? StartTime { get; set; }

        [JsonPropertyName("endTime")]
        public DateTime? EndTime { get; set; }

        [JsonPropertyName("durationMs")]
        [Range(long.MinValue, long.MaxValue)]
        public long? DurationMs { get; set; }

        [JsonPropertyName("message")]
        [StringLength(8000)]
        public string Message { get; set; } = "";

    }
}