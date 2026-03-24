using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Airport.Models
{
    public class Flight
    {
        public int Id { get; set; }
        public string FlightNumber { get; set; } = null!;
        public DateTime DepartureTime { get; set; }
        public int AvailableSeats { get; set; }
        public decimal Price { get; set; }
        public int AircraftId { get; set; }
        
        [ValidateNever]
        public Aircraft Aircraft { get; set; } = null!;
        
        [ValidateNever]
        public ICollection<Landing> Landings { get; set; } = new List<Landing>();
        
        [ValidateNever]
        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public int LoadPercentage { get; set; }
    }
} 