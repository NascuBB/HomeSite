using HomeSite.Entities;
using System.Text;
using System;
using HomeSite.Helpers;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;

namespace HomeSite.Managers
{
    public class UserPasswordManager
    {
        private List<PasswordReset> passwordResets;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly Timer _cleanupTimer;
        private readonly Random random;
        public UserPasswordManager(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
            _cleanupTimer = new Timer(_ => CleanupExpired(), null, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));
            passwordResets = new List<PasswordReset>();
            random = new Random();
        }

        public async Task SendPasswordReset(UserAccount user)
        {
            string code = GenerateCode();
            passwordResets.Add(new PasswordReset
            {
                ExpireTime = DateTime.UtcNow.AddHours(1),
                ResetCode = code,
                User = user
            });
            await EmailManager.SendPassRestoreEmailAsync(user.Email, code);
        }
        public bool IsResetCodeSent(UserAccount user)
        {
            return passwordResets.Any(x => x.User.Id == user.Id);
        }
        public bool CheckResetCode(string code)
        {
            return passwordResets.Any(r => r.ResetCode == code);
        }

        public async Task<bool> ResetPassword(string code, string newPassword)
        {
            if(!CheckResetCode(code)) return false;

            using var scope = _scopeFactory.CreateScope();
            var _userContext = scope.ServiceProvider.GetRequiredService<UserDBContext>();
            var pr = passwordResets.First(x => x.ResetCode == code);


            var user = _userContext.UserAccounts.Find(pr.User.Id)!;
            user.PasswordHash = SecurePasswordHasher.Hash(newPassword);
            await _userContext.SaveChangesAsync();
            passwordResets.Remove(pr);
            return true;
        }

        public async Task<bool> ChangePassword(UserAccount user, string oldPass, string newPass)
        {
            if(!SecurePasswordHasher.Verify(oldPass, user.PasswordHash)) return false;

            using var scope = _scopeFactory.CreateScope();
            var _userContext = scope.ServiceProvider.GetRequiredService<UserDBContext>();
            _userContext.Attach(user);
            user.PasswordHash = SecurePasswordHasher.Hash(newPass);
            await _userContext.SaveChangesAsync();
            return true;
        }

        private void CleanupExpired()
        {
            passwordResets.RemoveAll(x => x.ExpireTime < DateTime.UtcNow);
        }

        private string GenerateCode(int length = 8)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            var sb = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                sb.Append(chars[random.Next(chars.Length)]);
            }
            return sb.ToString();
        }
    }

    public class PasswordReset()
    {
        public required UserAccount User { get; set; }
        public DateTime ExpireTime { get; set; }
        public required string ResetCode { get; set; }
    }
}
