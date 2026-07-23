using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable enable

namespace Domain.Entities.Workspace
{
    [Table("PackageExecution", Schema = "dbo")]
    public class PackageExecution
    {
        // ExecutionId
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("ExecutionId")]
        public int ExecutionId { get; set; }

        // FileId
        [Required]
        [Column("FileId")]
        [MaxLength(20)]
        public string FileId { get; set; }

        // FileName
        [Required]
        [Column("FileName")]
        [MaxLength(520)]
        public string FileName { get; set; }

        // Status
        [Required]
        [Column("Status")]
        [MaxLength(40)]
        public string Status { get; set; }

        // StartTime
        [Column("StartTime")]
        public DateTime? StartTime { get; set; }

        // EndTime
        [Column("EndTime")]
        public DateTime? EndTime { get; set; }

        // DurationMs
        [Column("DurationMs")]
        public long? DurationMs { get; set; }

        // Message
        [Column("Message")]
        [MaxLength(8000)]
        public string Message { get; set; }

    }
}