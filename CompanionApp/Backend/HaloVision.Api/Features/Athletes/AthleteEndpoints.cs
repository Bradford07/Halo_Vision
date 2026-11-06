// Written with Claude Code, reviewed by Owen Cespon
using System.Security.Claims;
using HaloVision.Api.Data;
using HaloVision.Shared;
using Microsoft.EntityFrameworkCore;

namespace HaloVision.Api.Features.Athletes;

// The guide's athlete profiles: list, read, create, update, delete (P3's ProfileManager:
// listProfiles, createProfile, updateProfile, deleteProfile).
//
// Every endpoint only ever touches the signed-in guide's own athletes. Asking for another
// guide's athlete answers 404 Not Found, exactly as if it didn't exist, so nobody can even
// learn which profile IDs belong to other guides.
public static class AthleteEndpoints
{
    public static void MapAthleteEndpoints(this IEndpointRouteBuilder app)
    {
        // All routes start with /api/athletes and need a signed-in guide.
        var group = app.MapGroup("/api/athletes").RequireAuthorization(AuthPolicies.Guide);

        group.MapGet("/", ListAthletes);
        group.MapGet("/{profileId:int}", GetAthlete);
        group.MapPost("/", CreateAthlete);
        group.MapPut("/{profileId:int}", UpdateAthlete);
        group.MapDelete("/{profileId:int}", DeleteAthlete);
    }

    // GET /api/athletes: all of this guide's athletes.
    private static async Task<IResult> ListAthletes(ClaimsPrincipal user, AppDbContext db)
    {
        var athletes = await OwnAthletes(db, user).OrderBy(p => p.ProfileID).ToListAsync();
        return Results.Ok(athletes);
    }

    // GET /api/athletes/5: one athlete, or 404.
    private static async Task<IResult> GetAthlete(int profileId, ClaimsPrincipal user, AppDbContext db)
    {
        var athlete = await OwnAthletes(db, user).SingleOrDefaultAsync(p => p.ProfileID == profileId);
        return athlete is null ? Results.NotFound() : Results.Ok(athlete);
    }

    // POST /api/athletes: create a new athlete for this guide.
    private static async Task<IResult> CreateAthlete(AthleteProfile input, ClaimsPrincipal user, AppDbContext db)
    {
        var errors = AthleteProfileRules.Validate(input);
        if (errors.Count > 0)
            return Results.ValidationProblem(errors);

        // Start from a fresh profile and copy only the editable fields,
        // so a client can't choose its own ProfileID or Version.
        var athlete = new AthleteProfile
        {
            Version = 1,
            UpdatedAt = DateTime.UtcNow,
        };
        AthleteProfileRules.CopyEditableFields(input, athlete);

        db.AthleteProfiles.Add(athlete);
        db.Entry(athlete).Property(AppDbContext.GuideIdColumn).CurrentValue = GuideId(user);
        await db.SaveChangesAsync();

        return Results.Created($"/api/athletes/{athlete.ProfileID}", athlete);
    }

    // PUT /api/athletes/5: save the guide's edits. Each save raises Version by 1.
    private static async Task<IResult> UpdateAthlete(
        int profileId, AthleteProfile input, ClaimsPrincipal user, AppDbContext db)
    {
        var athlete = await OwnAthletes(db, user).SingleOrDefaultAsync(p => p.ProfileID == profileId);
        if (athlete is null)
            return Results.NotFound();

        var errors = AthleteProfileRules.Validate(input);
        if (errors.Count > 0)
            return Results.ValidationProblem(errors);

        AthleteProfileRules.CopyEditableFields(input, athlete);
        athlete.Version += 1;
        athlete.UpdatedAt = DateTime.UtcNow;

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // The headset saved this profile between our read and our write (Version changed).
            // Don't overwrite its change: send back the server's current copy instead.
            var current = await db.AthleteProfiles.AsNoTracking().SingleAsync(p => p.ProfileID == profileId);
            return Results.Conflict(current);
        }

        return Results.Ok(athlete);
    }

    // DELETE /api/athletes/5: remove the athlete (and, through the database links, its headsets).
    private static async Task<IResult> DeleteAthlete(int profileId, ClaimsPrincipal user, AppDbContext db)
    {
        var athlete = await OwnAthletes(db, user).SingleOrDefaultAsync(p => p.ProfileID == profileId);
        if (athlete is null)
            return Results.NotFound();

        db.AthleteProfiles.Remove(athlete);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    // The signed-in guide's user id (Identity puts it in the NameIdentifier claim).
    private static string GuideId(ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("Signed-in guide has no user id.");

    // Starting point for every query: only athletes whose hidden GuideId column is this guide.
    // Because every endpoint starts here, guide isolation is enforced in one place.
    private static IQueryable<AthleteProfile> OwnAthletes(AppDbContext db, ClaimsPrincipal user)
    {
        var guideId = GuideId(user);
        return db.AthleteProfiles.Where(p => EF.Property<string>(p, AppDbContext.GuideIdColumn) == guideId);
    }
}
