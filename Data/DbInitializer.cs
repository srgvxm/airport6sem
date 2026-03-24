using Airport.Models;
using System.Security.Cryptography;
using System.Text;

namespace Airport.Data
{
    public static class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context)
        {
            context.Database.EnsureCreated();
        }
    }
} 