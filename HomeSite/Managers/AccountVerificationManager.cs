using HomeSite.Entities;
using HomeSite.Helpers;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using static System.Formats.Asn1.AsnWriter;

namespace HomeSite.Managers
{
    public class AccountVerificationManager
    {
        //private readonly ConcurrentDictionary<string, bool> _requireVerification;
        public List<VerificateUser> UserCode { get; private set; }
        private readonly Random random;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IMemoryCache _cache;
        private readonly Timer _cleanupTimer;

        public AccountVerificationManager(IMemoryCache cache, IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
            _cache = cache;
            UserCode = new ();
            random = new ();
            _cleanupTimer = new Timer(_ => CleanupExpired(), null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }

        /// <summary>
        /// Checks if user needs verificate his email and caches user if he verified
        /// </summary>
        /// <param name="user">user</param>
        /// <returns><see cref="bool"/> true if needs verification, otherwise false</returns>
        public bool CheckVerification(UserAccount user)
        {
            if (user.DateLogged == null) return true;
            if (UserCode.Any(x => x.User.Id == user.Id)) return true;
            if (!user.Verified) return true;
            if ((DateTime.UtcNow - user.DateLogged.Value).TotalDays > 30) return true;

            _cache.Set($"{user.Username}", true, TimeSpan.FromSeconds(1));
            return false;
        }

        /// <summary>
        /// quick check if needs verification
        /// </summary>
        public bool RequiresVerification(string username)
        {
            return !_cache.GetOrCreate($"{username}", entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(1);
                using var scope = _scopeFactory.CreateScope();
                var _userContext = scope.ServiceProvider.GetRequiredService<UserDBContext>();
                var user = _userContext.UserAccounts.First(x => x.Username == username);
                if (user == null) return false;

                if (!user.Verified) return false; // пользователь не подтвержден

                if (user.DateLogged == null) return false;

                return (DateTime.UtcNow - user.DateLogged.Value).TotalDays <= 30;
            });
        }

        public async void SendVerificate(UserAccount user)
        {
            int code = GenerateCode();
            UserCode.Add(new VerificateUser
            {
                User = user,
                ExpireTime = DateTime.UtcNow.AddMinutes(10),
                VerificationCode = code
            });
            await EmailManager.SendCodeEmailAsync(user.Email, code);
        }

        public bool Verificate(UserAccount user, int code)
        {
            VerificateUser? vu = UserCode.FirstOrDefault(x => x.User.Id == user.Id);
            if (vu == null) return true;
            if (vu.VerificationCode == code)
            {
                _cache.Set($"{user.Username}", true, TimeSpan.FromSeconds(1));
                UserCode.Remove(vu);
                return true;
            }
            else return false;
        }
        private void CleanupExpired()
        {
            UserCode.RemoveAll(x => x.ExpireTime < DateTime.UtcNow);
        }
        private int GenerateCode()
        {
            return random.Next(100000, 999999);
        }
    }

    public class VerificateUser
    {
        public required UserAccount User { get; set; }
        public required int VerificationCode { get; set; }
        public DateTime ExpireTime { get; set; }
    }
}
