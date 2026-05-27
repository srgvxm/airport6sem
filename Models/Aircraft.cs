using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
namespace Airport.Models
{
    public class Aircraft
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Category { get; set; } = null!;
        public int SeatCount { get; set; }
        [ValidateNever]
        public ICollection<Flight> Flights { get; set; } = new List<Flight>();
    }
} 

