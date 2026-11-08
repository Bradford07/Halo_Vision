using System.Security.Claims;
using HaloVision.Api.Auth;
using HaloVision.Api.Data;
using HaloVision.Shared;
using Microsoft.EntityFrameworkCore;

namespace HaloVision.Api.Features.Athletes;

// The signed-in athlete's own profile: one account = one profile.
//
//   GET /api/my-profile  -> your profile (404 if you haven't made one yet)
//   PUT /api/my-profile  -> create your profile the first time, update it after that
//
// There's no profile id in the address. The server always uses the caller's own profile,
// so nobody can ever ask for someone else's.
public static class MyProfileEndpoints
{
    public static void MapMyProfileEndpoints(this IEndpointRouteBuilder app)
    {
        // Only a signed-in athlete can call these.
        var group = app.MapGroup("/api/my-profile").RequireAuthorization(AuthPolicies.Athlete);

        group.MapGet("/", GetMyProfile);
        group.MapPut("/", SaveMyProfile);
    }

    // GET /api/my-profile
    private static async Task<IResult> GetMyProfile(ClaimsPrincipal user, AppDbContext db)
    {
        var profile = await MyProfile(db, user).AsNoTracking().SingleOrDefaultAsync();
        return profile is null ? Results.NotFound() : Results.Ok(profile);
    }

    // PUT /api/my-profile   body: the whole profile
    // Every save raises Version by 1, so the headset can tell the web app's copy is newer.
    private static async Task<IResult> SaveMyProfile(AthleteProfile input, ClaimsPrincipal user, AppDbContext db)
    {
        var errors = AthleteProfileRules.Validate(input);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var profile = await MyProfile(db, user).SingleOrDefaultAsync();
        if (profile is null)
        {
            // First save: create the profile and link it to this account.
            profile = new AthleteProfile { Version = 1 };
            db.AthleteProfiles.Add(profile);
            db.Entry(profile).Property(AppDbContext.UserIdColumn).CurrentValue = user.GetUserId();
        }
        else
        {
            profile.Version += 1;
        }

        // Copy only the fields a person can edit; the server controls ProfileID, Version and UpdatedAt.
        AthleteProfileRules.CopyEditableFields(input, profile);
        profile.UpdatedAt = DateTime.UtcNow;

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // The headset saved this profile between our read and our write (Version changed).
            // Don't overwrite its change: send back the current copy instead.
            var current = await MyProfile(db, user).AsNoTracking().SingleAsync();
            return Results.Conflict(current);
        }

        return Results.Ok(profile);
    }

    // The signed-in athlete's profile: the one whose hidden UserId column is their account id.
    private static IQueryable<AthleteProfile> MyProfile(AppDbContext db, ClaimsPrincipal user)
    {
        var userId = user.GetUserId();
        return db.AthleteProfiles.Where(p => EF.Property<string>(p, AppDbContext.UserIdColumn) == userId);
    }
}
