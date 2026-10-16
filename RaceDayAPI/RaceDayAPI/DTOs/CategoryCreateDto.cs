namespace RaceDayApi.DTOs
{
    public class CategoryCreateDto
    {
        
            public int EventId { get; set; }
            public string Name { get; set; } = string.Empty;
            public decimal DistanceKm { get; set; }
            public decimal EntryFee { get; set; }
            public int MaxParticipants { get; set; }
        }
    }

