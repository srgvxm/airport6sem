using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Airport.Models
{
    public class Landing
    {
        public int Id { get; set; }
        public string Location { get; set; } = null!;
        public DateTime Time { get; set; }
        public int FlightId { get; set; }
        
        [ValidateNever]
        public Flight Flight { get; set; } = null!;
    }
} 