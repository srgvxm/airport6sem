using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Airport.Data;
using Airport.Models;
using Airport.Services;
using System.Security.Claims;
namespace Airport.Controllers
{
    [Authorize]
    public class TicketController : Controller
    {
        private readonly ApplicationDbContext _context;
        public TicketController(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            IQueryable<Ticket> ticketsQuery = _context.Tickets.Include(t => t.Flight);
            if (!User.IsInRole("Admin"))
            {
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdStr, out int userId))
                {
                    ticketsQuery = ticketsQuery.Where(t => t.UserId == userId);
                }
                else
                {
                    ticketsQuery = ticketsQuery.Where(t => false);
                }
            }
            var tickets = await ticketsQuery.ToListAsync();
            foreach (var ticket in tickets)
            {
                ParseDocumentNumber(ticket);
            }
            if (User.IsInRole("Admin"))
            {
                ViewData["IsAdminLayout"] = true;
            }
            return View(tickets);
        }
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var ticket = await _context.Tickets
                .Include(t => t.Flight)
                    .ThenInclude(f => f.Aircraft)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (ticket == null)
            {
                return NotFound();
            }
            ParseDocumentNumber(ticket);
            if (!User.IsInRole("Admin"))
            {
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdStr, out int userId) || ticket.UserId != userId)
                {
                    return Forbid();
                }
            }
            else
            {
                ViewData["IsAdminLayout"] = true;
            }
            return View(ticket);
        }
        private void ParseDocumentNumber(Ticket ticket)
        {
            if (!string.IsNullOrEmpty(ticket.DocumentNumber))
            {
                var parts = ticket.DocumentNumber.Split(' ');
                if (parts.Length == 2)
                {
                    ticket.PassportSeries = parts[0];
                    ticket.PassportNumber = parts[1];
                }
            }
        }
        public IActionResult Create(int? flightId)
        {
            if (User.IsInRole("Admin"))
            {
                ViewData["IsAdminLayout"] = true;
            }
            var availableFlights = _context.Flights
                .Where(f => f.AvailableSeats > 0)
                .Select(f => new
                {
                    Id = f.Id,
                    DisplayText = $"{f.FlightNumber} - {f.DepartureTime} ({f.AvailableSeats} мест)"
                });
            ViewData["FlightId"] = new SelectList(availableFlights, "Id", "DisplayText", flightId);
            var ticket = new Ticket 
            { 
                Date = DateTime.Today,
                Time = DateTime.Now.TimeOfDay,
                CashboxNumber = User.IsInRole("Admin") ? "1" : "0",
                PurchaseSource = User.IsInRole("Admin") ? "Offline" : "Online"
            };
            if (flightId.HasValue)
            {
                ticket.FlightId = flightId.Value;
            }
            return View(ticket);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,CashboxNumber,FlightId,Date,Time,PassengerName,PassportSeries,PassportNumber,ContactPhone,ContactEmail,SeatNumber")] Ticket ticket)
        {
            if (User.IsInRole("Admin"))
            {
                ViewData["IsAdminLayout"] = true;
            }
            if (!string.IsNullOrEmpty(ticket.PassportSeries) && !string.IsNullOrEmpty(ticket.PassportNumber))
            {
                ticket.DocumentNumber = $"{ticket.PassportSeries} {ticket.PassportNumber}";
            }
            if (ModelState.IsValid)
            {
                var flight = await _context.Flights.FindAsync(ticket.FlightId);
                if (flight == null || flight.AvailableSeats <= 0)
                {
                    ModelState.AddModelError("FlightId", "Нет свободных мест на данном рейсе");
                    var availableFlightsList = _context.Flights
                        .Where(f => f.AvailableSeats > 0)
                        .Select(f => new
                        {
                            Id = f.Id,
                            DisplayText = $"{f.FlightNumber} - {f.DepartureTime} ({f.AvailableSeats} мест)"
                        });
                    ViewData["FlightId"] = new SelectList(availableFlightsList, "Id", "DisplayText", ticket.FlightId);
                    return View(ticket);
                }
                ticket.TicketNumber = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
                if (string.IsNullOrEmpty(ticket.SeatNumber))
                {
                    var random = new Random();
                    ticket.SeatNumber = $"{random.Next(1, 31)}{(char)random.Next('A', 'G')}";
                }
                if (!User.IsInRole("Admin"))
                {
                    var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (int.TryParse(userIdStr, out int userId))
                    {
                        ticket.UserId = userId;
                    }
                    ticket.PurchaseSource = "Online";
                    ticket.CashboxNumber = "0";
                }
                else
                {
                    ticket.PurchaseSource = "Offline";
                    if (string.IsNullOrEmpty(ticket.CashboxNumber))
                    {
                        ticket.CashboxNumber = "1";
                    }
                }
                ticket.Status = "Paid";
                flight.AvailableSeats--;
                _context.Update(flight);
                _context.Add(ticket);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            var availableFlights = _context.Flights
                .Where(f => f.AvailableSeats > 0)
                .Select(f => new
                {
                    Id = f.Id,
                    DisplayText = $"{f.FlightNumber} - {f.DepartureTime} ({f.AvailableSeats} мест)"
                });
            ViewData["FlightId"] = new SelectList(availableFlights, "Id", "DisplayText", ticket.FlightId);
            return View(ticket);
        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            ViewData["IsAdminLayout"] = true;
            if (id == null)
            {
                return NotFound();
            }
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null)
            {
                return NotFound();
            }
            var flights = _context.Flights.Select(f => new
            {
                Id = f.Id,
                DisplayText = $"{f.FlightNumber} - {f.DepartureTime} ({f.AvailableSeats} мест)"
            });
            ViewData["FlightId"] = new SelectList(flights, "Id", "DisplayText", ticket.FlightId);
            return View(ticket);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CashboxNumber,FlightId,Date,Time,PassengerName,PassportSeries,PassportNumber,ContactPhone,ContactEmail,SeatNumber")] Ticket ticket)
        {
            ViewData["IsAdminLayout"] = true;
            if (id != ticket.Id)
            {
                return NotFound();
            }
            if (!string.IsNullOrEmpty(ticket.PassportSeries) && !string.IsNullOrEmpty(ticket.PassportNumber))
            {
                ticket.DocumentNumber = $"{ticket.PassportSeries} {ticket.PassportNumber}";
            }
            if (ModelState.IsValid)
            {
                try
                {
                    var originalTicket = await _context.Tickets
                        .AsNoTracking()
                        .FirstOrDefaultAsync(t => t.Id == id);
                    if (originalTicket != null && originalTicket.FlightId != ticket.FlightId)
                    {
                        var oldFlight = await _context.Flights.FindAsync(originalTicket.FlightId);
                        if (oldFlight != null)
                        {
                            oldFlight.AvailableSeats++;
                            _context.Update(oldFlight);
                        }
                        var newFlight = await _context.Flights.FindAsync(ticket.FlightId);
                        if (newFlight != null)
                        {
                            if (newFlight.AvailableSeats <= 0)
                            {
                                ModelState.AddModelError("FlightId", "Нет свободных мест на выбранном рейсе");
                                var flights = _context.Flights.Select(f => new
                                {
                                    Id = f.Id,
                                    DisplayText = $"{f.FlightNumber} - {f.DepartureTime} ({f.AvailableSeats} мест)"
                                });
                                ViewData["FlightId"] = new SelectList(flights, "Id", "DisplayText", ticket.FlightId);
                                return View(ticket);
                            }
                            newFlight.AvailableSeats--;
                            _context.Update(newFlight);
                        }
                    }
                    if (originalTicket != null)
                    {
                        ticket.TicketNumber = originalTicket.TicketNumber;
                        ticket.PurchaseSource = originalTicket.PurchaseSource;
                        ticket.Status = originalTicket.Status;
                        ticket.UserId = originalTicket.UserId;
                    }
                    _context.Update(ticket);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TicketExists(ticket.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            var availableFlights = _context.Flights.Select(f => new
            {
                Id = f.Id,
                DisplayText = $"{f.FlightNumber} - {f.DepartureTime} ({f.AvailableSeats} мест)"
            });
            ViewData["FlightId"] = new SelectList(availableFlights, "Id", "DisplayText", ticket.FlightId);
            return View(ticket);
        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            ViewData["IsAdminLayout"] = true;
            if (id == null)
            {
                return NotFound();
            }
            var ticket = await _context.Tickets
                .Include(t => t.Flight)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (ticket == null)
            {
                return NotFound();
            }
            if (Request.Method == "POST")
            {
                var flight = ticket.Flight;
                if (flight != null)
                {
                    flight.AvailableSeats++;
                    _context.Update(flight);
                }
                _context.Tickets.Remove(ticket);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(ticket);
        }
        private bool TicketExists(int id)
        {
            return _context.Tickets.Any(e => e.Id == id);
        }
    }
} 

