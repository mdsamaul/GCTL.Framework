using GCTL.Core.Data;
using GCTL.Data.Models;
using Microsoft.AspNetCore.Identity;
using Serilog;
using System.Text;

namespace GCTL.UI.Core.Helpers
{
    public static class LegacyPasswordMigration
    {
        public static async Task RunAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var userRepo = scope.ServiceProvider.GetRequiredService<IRepository<CoreUserInfo>>();
            var hasher = new PasswordHasher<CoreUserInfo>();

            var allUsers = await userRepo.GetAllAsync();
            int migrated = 0, failed = 0;

            foreach (var user in allUsers)
            {
                if (string.IsNullOrEmpty(user.Password)) continue;
                if (!LegacyPasswordHelper.IsLegacyFormat(user.Password)) continue; // already modern, skip

                try
                {
                    var plain = LegacyPasswordHelper.Decode(user.Password);
                    user.Password = hasher.HashPassword(user, plain);
                    await userRepo.UpdateAsync(user);
                    migrated++;
                }
                catch
                {
                    failed++;
                    Log.Warning("Password migration failed for user {Username}", user.Username);
                }
            }

            Log.Information("Password migration complete: {Migrated} migrated, {Failed} failed", migrated, failed);
        }
    }
    public static class LegacyPasswordHelper
    {
        private const string Prefix = "PXPollab";
        private const string Suffix = "Y2013";

        public static string Decode(string encoded)
        {
            var layer1 = Encoding.Unicode.GetString(Convert.FromBase64String(encoded));
            var layer2 = Encoding.Unicode.GetString(Convert.FromBase64String(layer1));

            if (!layer2.StartsWith(Prefix) || !layer2.EndsWith(Suffix))
            {

            }

            return layer2.Substring(Prefix.Length, layer2.Length - Prefix.Length -Suffix.Length);
        }

        public static bool IsLegacyFormat(string stored)
        {
            if (string.IsNullOrEmpty(stored)) return false;

            try
            {
                var layer1 = Encoding.Unicode.GetString(Convert.FromBase64String(stored));
                var layer2 = Encoding.Unicode.GetString(Convert.FromBase64String(layer1));
                return layer2.StartsWith(Prefix) && layer2.EndsWith(Suffix);
            }
            catch
            {
                return false;
            }
        }
    }
}
