using System.ComponentModel.DataAnnotations;

namespace RaceDayApi.DTOs
{
    public class ResultCreateDto
    {
        [Required]
        public int EnrolmentId { get; set; }

        public TimeSpan? FinishTime { get; set; }

        public int? Position { get; set; }

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = string.Empty;
    }
}