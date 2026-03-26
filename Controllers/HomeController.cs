using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Airport.Models;
using Airport.Data;
using Microsoft.EntityFrameworkCore;

namespace Airport.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public async Task<IActionResult> SearchFlights(string destination, DateTime? departureDate)
        {
            if (!departureDate.HasValue)
            {
                departureDate = DateTime.Today;
            }

            var flights = await _context.Flights
                .Include(f => f.Aircraft)
                .Include(f => f.Landings)
                .Where(f => f.AvailableSeats > 0 && 
                            f.DepartureTime >= departureDate.Value &&
                            (f.Landings.Any(l => l.Location.Contains(destination)) || 
                             f.FlightNumber.Contains(destination)))
                .OrderBy(f => f.DepartureTime)
                .ToListAsync();

            ViewBag.Destination = destination;
            ViewBag.DepartureDate = departureDate.Value.ToString("yyyy-MM-dd");
            
            return View(flights);
        }

        public async Task<IActionResult> TicketSalesReport(DateTime startDate, DateTime endDate)
        {
            if (endDate < startDate)
            {
                endDate = startDate.AddMonths(1);
            }

            var tickets = await _context.Tickets
                .Include(t => t.Flight)
                .Include(t => t.Flight.Aircraft)
                .Where(t => t.Date >= startDate && t.Date <= endDate)
                .OrderBy(t => t.Date)
                .ThenBy(t => t.Time)
                .ToListAsync();

            ViewBag.StartDate = startDate.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate.ToString("yyyy-MM-dd");
            ViewBag.TotalSales = tickets.Sum(t => t.Flight.Price);
            ViewBag.TicketCount = tickets.Count;

            return View(tickets);
        }

        public async Task<IActionResult> FlightLoadReport()
        {
            var flights = await _context.Flights
                .Include(f => f.Aircraft)
                .Where(f => f.DepartureTime >= DateTime.Today)
                .OrderBy(f => f.DepartureTime)
                .ToListAsync();

            foreach (var flight in flights)
            {
                flight.LoadPercentage = 100 - (flight.AvailableSeats * 100 / flight.Aircraft.SeatCount);
            }

            return View(flights);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    public class ErrorViewModel
    {
        public string? RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
} 