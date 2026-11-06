// Written with Claude Code, reviewed by Owen Cespon
using System.Net;
using HaloVision.Api.Data;
using HaloVision.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HaloVision.Api.Tests;

// Checks that the database, the migrations, and the test factory all work together.
public class DatabaseTests
{
    // The app starts with the test database and answers /health.
    [Fact]
    public async Task Health_returns_200()
    {
        using var factory = new TestAppFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // An athlete profile saved for a guide comes back with the same values and the same guide.
    [Fact]
    public async Task Athlete_profile_is_saved_and_loaded_with_its_guide()
    {
        using var factory = new TestAppFactory();

        // Arrange + Act: save a guide and one of their athletes.
        int profileId;
        string guideId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = factory.GetDb(scope);
            var guide = new IdentityUser { UserName = "test-guide@example.com" };
            db.Users.Add(guide);

            var athlete = new AthleteProfile { Name = "Test Athlete", SurfaceMode = SurfaceMode.Dirt };
            db.AthleteProfiles.Add(athlete);
            db.Entry(athlete).Property(AppDbContext.GuideIdColumn).CurrentValue = guide.Id;

            await db.SaveChangesAsync();
            profileId = athlete.ProfileID;
            guideId = guide.Id;
        }

        // Assert: load it in a fresh scope, so it really comes from the database, not memory.
        using (var scope = factory.Services.CreateScope())
        {
            var db = factory.GetDb(scope);
            var loaded = await db.AthleteProfiles.SingleAsync(p => p.ProfileID == profileId);

            Assert.True(profileId > 0);                      // the database assigned an ID
            Assert.Equal("Test Athlete", loaded.Name);
            Assert.Equal(SurfaceMode.Dirt, loaded.SurfaceMode);
            Assert.Equal(guideId, db.Entry(loaded).Property(AppDbContext.GuideIdColumn).CurrentValue);
        }
    }

    // Enums are stored as readable text, not numbers.
    [Fact]
    public async Task Enums_are_stored_as_text()
    {
        using var factory = new TestAppFactory();
        using var scope = factory.Services.CreateScope();
        var db = factory.GetDb(scope);

        var guide = new IdentityUser { UserName = "test-guide@example.com" };
        db.Users.Add(guide);
        var athlete = new AthleteProfile { VisionLossType = VisionLossType.Central };
        db.AthleteProfiles.Add(athlete);
        db.Entry(athlete).Property(AppDbContext.GuideIdColumn).CurrentValue = guide.Id;
        await db.SaveChangesAsync();

        // Read the raw column with SQL to see exactly what the database stored.
        var stored = await db.Database
            .SqlQueryRaw<string>("SELECT VisionLossType AS Value FROM athlete_profile")
            .SingleAsync();

        Assert.Equal("Central", stored);
    }

    // A profile can't be saved without a guide: every athlete must belong to someone.
    [Fact]
    public async Task Athlete_profile_without_a_guide_is_rejected()
    {
        using var factory = new TestAppFactory();
        using var scope = factory.Services.CreateScope();
        var db = factory.GetDb(scope);

        db.AthleteProfiles.Add(new AthleteProfile { Name = "No Guide" });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
}
