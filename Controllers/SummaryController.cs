using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Airport.Data;
using Airport.Models;
using Airport.ViewModels;
using Airport.Services;
namespace Airport.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SummaryController : BaseController
    {
        private readonly ApplicationDbContext _context;
        public SummaryController(ApplicationDbContext context, NotificationService notificationService)
            : base(notificationService)
        {
            _context = context;
        }
        public async Task<IActionResult> Aircraft()
        {
            var aircraftSummaries = await _context.Aircrafts
                .Select(a => new AircraftSummaryViewModel
                {
                    Id = a.Id,
                    Model = a.Name,
                    Manufacturer = a.Category,
                    TotalSeats = a.SeatCount,
                    FlightsCount = a.Flights.Count
                })
                .ToListAsync();
            return View(aircraftSummaries);
        }
        public async Task<IActionResult> Flights()
        {
            var flightSummaries = await _context.Flights
                .Include(f => f.Aircraft)
                .Include(f => f.Landings)
                .Select(f => new FlightSummaryViewModel
                {
                    Id = f.Id,
                    FlightNumber = f.FlightNumber,
                    DepartureTime = f.DepartureTime,
                    Aircraft = f.Aircraft.Name,
                    AvailableSeats = f.AvailableSeats,
                    TotalSeats = f.Aircraft.SeatCount,
                    Landings = f.Landings.Select(l => new LandingSummaryViewModel
                    {
                        Location = l.Location,
                        Time = l.Time.TimeOfDay
                    }).ToList()
                })
                .ToListAsync();
            return View(flightSummaries);
        }
        public async Task<IActionResult> Tickets()
        {
            var ticketSummaries = await _context.Tickets
                .Include(t => t.Flight)
                .Include(t => t.Flight.Aircraft)
                .Select(t => new TicketSummaryViewModel
                {
                    Id = t.Id,
                    CashboxNumber = t.CashboxNumber,
                    FlightNumber = t.Flight.FlightNumber,
                    AircraftModel = t.Flight.Aircraft.Name,
                    Date = t.Date,
                    Time = t.Time,
                    DepartureTime = t.Flight.DepartureTime
                })
                .ToListAsync();
            return View(ticketSummaries);
        }
    }
} 

