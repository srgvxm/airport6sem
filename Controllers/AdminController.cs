using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Airport.Data;
using Airport.Models;
using System.Linq;
using System.Threading.Tasks;
namespace Airport.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            var stats = new AdminDashboardViewModel
            {
                TotalAircrafts = await _context.Aircrafts.CountAsync(),
                TotalFlights = await _context.Flights.CountAsync(),
                TotalTickets = await _context.Tickets.CountAsync(),
                TotalLandings = await _context.Landings.CountAsync(),
                RecentTickets = await _context.Tickets
                    .Include(t => t.Flight)
                    .OrderByDescending(t => t.Id)
                    .Take(5)
                    .ToListAsync(),
                UpcomingFlights = await _context.Flights
                    .Include(f => f.Aircraft)
                    .Where(f => f.DepartureTime > System.DateTime.Now)
                    .OrderBy(f => f.DepartureTime)
                    .Take(5)
                    .ToListAsync()
            };
            return View(stats);
        }
        public async Task<IActionResult> Users()
        {
            var users = await _context.Users.ToListAsync();
            return View(users);
        }
        public async Task<IActionResult> UserDetails(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.Id == id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }
        public async Task<IActionResult> EditUser(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(int id, [Bind("Id,Username,Email,Role")] User user)
        {
            if (id != user.Id)
            {
                return NotFound();
            }
            var existingUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
            if (existingUser == null)
            {
                return NotFound();
            }
            user.PasswordHash = existingUser.PasswordHash;
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(user);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Users));
            }
            return View(user);
        }
        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }
    }
} 

