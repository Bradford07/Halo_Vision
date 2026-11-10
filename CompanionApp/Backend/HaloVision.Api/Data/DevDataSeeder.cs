using System.Security.Cryptography;
using HaloVision.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HaloVision.Api.Data;

// Fills an empty Development database with one fake athlete account and its profile,
// so there's something to try the API with. Program.cs only calls this in Development.
public static class DevDataSeeder
{
    public const string AthleteEmail = "athlete@example.com";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DevDataSeeder");

        // Only seed an empty database, so restarting doesn't add duplicates.
        if (await db.Users.AnyAsync())
        {
            return;
        }

        // A random password, so no password is ever committed to git.
        var password = $"Dev!{RandomNumberGenerator.GetHexString(12)}a1";

        // The fake athlete's account.
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
        var account = new IdentityUser { UserName = AthleteEmail, Email = AthleteEmail };
        var result = await userManager.CreateAsync(account, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException("Could not create the fake athlete account.");
        }

        // The fake athlete's profile (fake name only), linked to that account.
        var profile = new AthleteProfile { Name = "Sample Athlete", Height = 1.75f, UpdatedAt = DateTime.UtcNow };
        db.AthleteProfiles.Add(profile);
        db.Entry(profile).Property(AppDbContext.UserIdColumn).CurrentValue = account.Id;
        await db.SaveChangesAsync();

        logger.LogWarning(
            "Seeded fake dev data. Login: {Email} / {Password} (shown once; delete halovision.db to reseed).",
            AthleteEmail, password);
    }
}
