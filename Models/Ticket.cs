using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
namespace Airport.Models
{
    public class Ticket
    {
        public int Id { get; set; }
        [ValidateNever]
        public string TicketNumber { get; set; } = string.Empty;
        public string CashboxNumber { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public TimeSpan Time { get; set; }
        [ValidateNever]
        public string SeatNumber { get; set; } = string.Empty;
        [Required(ErrorMessage = "ФИО пассажира обязательно")]
        public string? PassengerName { get; set; }
        [NotMapped]
        [Required(ErrorMessage = "Серия паспорта обязательна")]
        [StringLength(4, MinimumLength = 4, ErrorMessage = "Серия паспорта должна состоять из 4 цифр")]
        [RegularExpression(@"^\d{4}$", ErrorMessage = "Серия паспорта должна содержать только цифры")]
        public string? PassportSeries { get; set; }
        [NotMapped]
        [Required(ErrorMessage = "Номер паспорта обязателен")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Номер паспорта должен состоять из 6 цифр")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Номер паспорта должен содержать только цифры")]
        public string? PassportNumber { get; set; }
        public string? DocumentNumber { get; set; }
        [Required(ErrorMessage = "Номер телефона обязателен")]
        [RegularExpression(@"^\+7\d{10}$", ErrorMessage = "Номер телефона должен быть в формате +71234567890 (всего 12 символов)")]
        public string? ContactPhone { get; set; }
        [EmailAddress(ErrorMessage = "Некорректный формат Email")]
        public string? ContactEmail { get; set; }
        [ValidateNever]
        public string PurchaseSource { get; set; } = "Offline";
        [ValidateNever]
        public string Status { get; set; } = "Paid";
        public int? UserId { get; set; }
        public int FlightId { get; set; }
        [ValidateNever]
        public Flight Flight { get; set; } = null!;

        public bool HasBaggage { get; set; }
        public bool HasMeal { get; set; }
        public bool HasInsurance { get; set; }

        [NotMapped] public static decimal BaggagePrice  => 1500m;
        [NotMapped] public static decimal MealPrice     => 500m;
        [NotMapped] public static decimal InsurancePrice => 800m;

        [NotMapped]
        public decimal ExtraServicesTotal =>
            (HasBaggage ? BaggagePrice : 0) +
            (HasMeal ? MealPrice : 0) +
            (HasInsurance ? InsurancePrice : 0);

        [NotMapped]
        public decimal TotalPrice => (Flight?.Price ?? 0) + ExtraServicesTotal;
    }
} 

