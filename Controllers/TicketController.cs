using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Airport.Data;
using Airport.Models;

namespace Airport.Controllers
{
    public class TicketController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TicketController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var tickets = await _context.Tickets
                .Include(t => t.Flight)
                .ToListAsync();
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

            return View(ticket);
        }

        public IActionResult Create(int? flightId)
        {
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
                Time = DateTime.Now.TimeOfDay
            };
            
            if (flightId.HasValue)
            {
                ticket.FlightId = flightId.Value;
            }
            
            return View(ticket);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,CashboxNumber,FlightId,Date,Time")] Ticket ticket)
        {
            if (ModelState.IsValid)
            {
                var flight = await _context.Flights.FindAsync(ticket.FlightId);
                if (flight == null || flight.AvailableSeats <= 0)
                {
                    ModelState.AddModelError("FlightId", "Нет свободных мест на данном рейсе");
                    return View(ticket);
                }

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

        public async Task<IActionResult> Edit(int? id)
        {
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
        public async Task<IActionResult> Edit(int id, [Bind("Id,CashboxNumber,FlightId,Date,Time")] Ticket ticket)
        {
            if (id != ticket.Id)
            {
                return NotFound();
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

        public async Task<IActionResult> Delete(int? id)
        {
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