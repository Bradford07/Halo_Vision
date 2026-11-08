// Written with Claude Code, reviewed by Owen Cespon
using System.Security.Cryptography;
using HaloVision.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HaloVision.Api.Data;

// Fills an empty Development database with fake data, so there's something to try the API with.
// Program.cs only calls this in the Development environment. Fake names only: never real athletes.
public static class DevDataSeeder
{
    // The fake guide's login name.
    public const string GuideEmail = "guide@example.com";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DevDataSeeder");

        // Build or update the database from the migrations (creates halovision.db the first time).
        await db.Database.MigrateAsync();

        // Only seed an empty database, so restarting the app doesn't add duplicates.
        if (await db.Users.AnyAsync())
        {
            return;
        }

        // A random password, so no password is ever committed to git (CONTRIBUTING rule 7).
        // The extra "Dev!" and "a1" make sure it meets Identity's rules (upper, lower, digit, symbol).
        var password = $"Dev!{RandomNumberGenerator.GetHexString(12)}a1";

        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
        var guide = new IdentityUser { UserName = GuideEmail, Email = GuideEmail, EmailConfirmed = true };
        var result = await userManager.CreateAsync(guide, password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Could not create the fake guide: {errors}");
        }

        // Two fake athletes owned by the fake guide.
        var athleteA = new AthleteProfile
        {
            Name = "Sample Athlete A",
            VisionLossType = VisionLossType.Central,
            Severity = Severity.Moderate,
            Height = 1.75f,
            SurfaceMode = SurfaceMode.Dirt,
            CueMode = CueMode.Both,
            Notes = "Fake athlete for development.",
            UpdatedAt = DateTime.UtcNow,
        };
        var athleteB = new AthleteProfile
        {
            Name = "Sample Athlete B",
            VisionLossType = VisionLossType.Peripheral,
            Severity = Severity.Mild,
            Height = 1.62f,
            Notes = "Fake athlete for development.",
            UpdatedAt = DateTime.UtcNow,
        };

        db.AthleteProfiles.AddRange(athleteA, athleteB);

        // GuideId isn't on the AthleteProfile class (it's a shadow property), so we set it through EF.
        db.Entry(athleteA).Property(AppDbContext.GuideIdColumn).CurrentValue = guide.Id;
        db.Entry(athleteB).Property(AppDbContext.GuideIdColumn).CurrentValue = guide.Id;

        await db.SaveChangesAsync();

        logger.LogWarning(
            "Seeded fake dev data. Guide login: {Email} / {Password} (shown once; delete halovision.db to reseed).",
            GuideEmail, password);
    }
}
