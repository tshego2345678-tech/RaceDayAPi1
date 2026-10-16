using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDayApi.Models
{
    [Table("Events")]
    public class Events
    {
        [Key]
        public int EventId { get; set; }

        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        public DateTime EventDate { get; set; }

        [Required]
        [StringLength(150)]
        public string Location { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        [Required]
        public int OrganiserId { get; set; }
    }
}