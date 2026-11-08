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


    //Profile saved for an account comes back with the same vaues and the same account
    [Fact]
    public async Task Profile_is_saved_and_loaded_with_its_account()
    {
        using var factory = new TestAppFactory();

        int profileId;
        string accountId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = factory.GetDb(scope);
            var account = new IdentityUser { UserName = "test-athlete@example.com" };
            db.Users.Add(account);

            var athlete = new AthleteProfile { Name = "Test Athlete", SurfaceMode = SurfaceMode.Dirt };
            db.AthleteProfiles.Add(athlete);
            db.Entry(athlete).Property(AppDbContext.UserIdColumn).CurrentValue = account.Id;

            await db.SaveChangesAsync();
            profileId = athlete.ProfileID;
            accountId = account.Id;
        }

        // Assert: load it in a fresh scope, so it really comes from the database, not memory.
        using (var scope = factory.Services.CreateScope())
        {
            var db = factory.GetDb(scope);
            var loaded = await db.AthleteProfiles.SingleAsync(p => p.ProfileID == profileId);

            Assert.True(profileId > 0);                      // the database assigned an ID
            Assert.Equal("Test Athlete", loaded.Name);
            Assert.Equal(SurfaceMode.Dirt, loaded.SurfaceMode);
            Assert.Equal(accountId, db.Entry(loaded).Property(AppDbContext.UserIdColumn).CurrentValue);
        }
    }

    
    // A profile can't be saved without an account: every profile must belong to someone. 
    [Fact]
    public async Task Profile_without_an_account_is_rejected()
    {
        using var factory = new TestAppFactory();
        using var scope = factory.Services.CreateScope();
        var db = factory.GetDb(scope);

        db.AthleteProfiles.Add(new AthleteProfile { Name = "No Account" });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    //One account = one profile: the database refuses a second profile for the same account.
    [Fact]
    public async Task Account_cannot_have_two_profiles()
    {
        using var factory = new TestAppFactory();
        using var scope = factory.Services.CreateScope();
        var db = factory.GetDb(scope);

        var account = new IdentityUser { UserName = "test-athlete@example.com" };
        db.Users.Add(account);
        var first = new AthleteProfile { Name = "First" };
        db.AthleteProfiles.Add(first);

        db.Entry(first).Property(AppDbContext.UserIdColumn).CurrentValue = account.Id;
        await db.SaveChangesAsync();

        //A second profile for the same account must fail.
        var second = new AthleteProfile { Name = "Second" };
        db.AthleteProfiles.Add(second);
        db.Entry(second).Property(AppDbContext.UserIdColumn).CurrentValue = account.Id;

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
}
