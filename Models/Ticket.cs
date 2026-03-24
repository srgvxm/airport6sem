using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Airport.Models
{
    public class Ticket
    {
        public int Id { get; set; }
        public string CashboxNumber { get; set; } = null!;
        public DateTime Date { get; set; }
        public TimeSpan Time { get; set; }
        public int FlightId { get; set; }
        
        [ValidateNever]
        public Flight Flight { get; set; } = null!;
    }
} 