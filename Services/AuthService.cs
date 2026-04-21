using System.Security.Cryptography;
using System.Text;
using Airport.Models;
using Airport.Data;
using Airport.ViewModels;
using Microsoft.EntityFrameworkCore;
namespace Airport.Services
{
    public interface IAuthService
    {
        Task<User> RegisterAsync(RegisterViewModel model);
        Task<User> LoginAsync(LoginViewModel model);
        Task<bool> UserExistsAsync(string email);
        Task ChangePasswordAsync(int userId, string currentPassword, string newPassword);
        Task<User> UpdateProfileAsync(int userId, string username, string email);
    }

    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;

        public AuthService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User> RegisterAsync(RegisterViewModel model)
        {
            if (await UserExistsAsync(model.Email))
                throw new Exception("Пользователь с таким email уже существует");

            var user = new User
            {
                Username     = model.Username,
                Email        = model.Email,
                PasswordHash = HashPassword(model.Password)
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<User> LoginAsync(LoginViewModel model)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (user == null || !VerifyPassword(model.Password, user.PasswordHash))
                throw new Exception("Неверный email или пароль");
            return user;
        }

        public async Task<bool> UserExistsAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }

        public async Task ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            var user = await _context.Users.FindAsync(userId)
                ?? throw new Exception("Пользователь не найден");

            if (!VerifyPassword(currentPassword, user.PasswordHash))
                throw new Exception("Текущий пароль указан неверно");

            user.PasswordHash = HashPassword(newPassword);
            await _context.SaveChangesAsync();
        }

        public async Task<User> UpdateProfileAsync(int userId, string username, string email)
        {
            var user = await _context.Users.FindAsync(userId)
                ?? throw new Exception("Пользователь не найден");

            var emailTaken = await _context.Users
                .AnyAsync(u => u.Email == email && u.Id != userId);
            if (emailTaken)
                throw new Exception("Этот email уже используется другим аккаунтом");

            user.Username = username;
            user.Email    = email;
            await _context.SaveChangesAsync();
            return user;
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        private bool VerifyPassword(string password, string hash)
            => HashPassword(password) == hash;
    }
}
