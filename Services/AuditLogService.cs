using Airport.Data;
using Airport.Filters;
using Airport.Models;
using Microsoft.EntityFrameworkCore;
namespace Airport.Services
{
    public class AuditLogService
    {
        private readonly ApplicationDbContext _context;

        public AuditLogService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(AuditLog entry)
        {
            _context.AuditLogs.Add(entry);
            await _context.SaveChangesAsync();
        }

        public async Task<(List<AuditLog> Items, int TotalCount)> GetLogsAsync(
            string? username = null,
            string? role = null,
            string? actionDescription = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            int page = 1,
            int pageSize = 50)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrEmpty(username))
                query = query.Where(a => a.Username.Contains(username));

            if (!string.IsNullOrEmpty(role))
                query = query.Where(a => a.UserRole == role);

            if (!string.IsNullOrEmpty(actionDescription))
                query = query.Where(a => a.ActionDescription == actionDescription);

            if (dateFrom.HasValue)
                query = query.Where(a => a.Timestamp >= dateFrom.Value);

            if (dateTo.HasValue)
                query = query.Where(a => a.Timestamp <= dateTo.Value.AddDays(1));

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(a => a.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public List<string> GetAllDescriptions()
        {
            return AuditActionFilter.Descriptions.Values
                .Distinct()
                .OrderBy(d => d)
                .ToList();
        }

        public async Task<List<string>> GetDistinctRolesAsync()
        {
            return await _context.AuditLogs
                .Select(a => a.UserRole)
                .Where(r => r != "")
                .Distinct()
                .OrderBy(r => r)
                .ToListAsync();
        }

        public async Task<object> GetStatsAsync()
        {
            var today = DateTime.UtcNow.Date;
            var totalToday = await _context.AuditLogs
                .CountAsync(a => a.Timestamp >= today);
            var totalAll = await _context.AuditLogs.CountAsync();
            var uniqueUsersToday = await _context.AuditLogs
                .Where(a => a.Timestamp >= today && a.UserId != null)
                .Select(a => a.UserId)
                .Distinct()
                .CountAsync();

            return new { totalToday, totalAll, uniqueUsersToday };
        }
    }
}
