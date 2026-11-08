using System.Security.Claims;
using HaloVision.Api.Auth;
using HaloVision.Api.Data;
using HaloVision.Api.Features.Athletes;
using HaloVision.Shared;
using Microsoft.EntityFrameworkCore;

namespace HaloVision.Api.Features.Sync;

//Profile sync for a paired headset
//GET /api/profile -> latest copy of this headset's athlete profile
//PUT /api/profile -> upload a change made on the headset

//Sync rule: the copy with the higher Version is newer. 
//The server saves the headset's copy only if its Version is higher. Otherwise answers 409

public static class ProfileSyncEndpoints
{
    public static void MapProfileSyncEndpoints(this IEndpointRouteBuilder app)
    {
        //Only a paired headset, logged in with its token can call these
        var group = app.MapGroup("/api/profile").RequireAuthorization(AuthPolicies.Device);

        group.MapGet("/", GetProfile);
        group.MapPut("/", SaveProfile);
    }

    //GET /api/profile
    private static async Task<IResult> GetProfile(ClaimsPrincipal user, AppDbContext db)
    {
        //The token says whcih profile this headset belongs to; the URL has no id
        var profile = await db.AthleteProfiles.FindAsync(user.GetDeviceProfileId());

        if(profile is null)
        {
            return Results.NotFound();
        }

        //Remember when the headset last synced
        var device = await db.Devices.FindAsync(user.GetDeviceId());
        device!.LastSyncAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Results.Ok(profile);
    }

    //PUT /api/profile body: the headset's whole copy, with its Version already raised by 1
    private static async Task<IResult> SaveProfile(AthleteProfile input, ClaimsPrincipal user, AppDbContext db)
    {
        //Same value cheks as the phone
        var errors = AthleteProfileRules.Validate(input);
        if(errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var profile = await db.AthleteProfiles.FindAsync(user.GetDeviceProfileId());
        if(profile is null)
        {
            return Results.NotFound();
        }


        // The sync rule. A tie or a lower number meands the servers copy changed first
        if(input.Version <= profile.Version)
        {
            return Results.Conflict(profile);
        }

        //The headset's copy is newer: save it, keeping the headset's Version number
        AthleteProfileRules.CopyEditableFields(input, profile);
        profile.Version = input.Version;
        profile.UpdatedAt = DateTime.UtcNow;

        var device = await db.Devices.FindAsync(user.GetDeviceId());
        device!.LastSyncAt = DateTime.UtcNow;

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            //Phone saved between our read and write. Its copy wins: send it back
            var current = await db.AthleteProfiles.AsNoTracking().SingleAsync(p => p.ProfileID == profile.ProfileID);

            return Results.Conflict(current);
        }

        return Results.Ok(profile);
    }
}