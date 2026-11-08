# Profile sync tests to write

**Branch:** `feature/profile-sync`
**New file:** `CompanionApp/Backend/HaloVision.Api.Tests/ProfileSyncTests.cs`

## Files to look at

| File | Why |
|---|---|
| `HaloVision.Api/Features/Sync/ProfileSyncEndpoints.cs` | The code being tested: `GET /api/profile` and `PUT /api/profile` |
| `HaloVision.Api/Features/Athletes/AthleteProfileRules.cs` | The value checks `PUT` uses (name, height, color, 0–1 ranges) |
| `HaloVision.Api.Tests/TestAppFactory.cs` | Helpers: `CreateAthleteClientAsync`, `SaveProfileAsync`, `PairHeadsetAsync`, `Json`, `GetDb` |
| `HaloVision.Api.Tests/PairingTests.cs`, `DeviceAuthTests.cs` | Examples of the same test style |

## The sync rule

The server saves the headset's copy only if its `Version` is **higher** than the server's.
A tie or a lower number gets **409**, and the response body is the server's copy.

## Setup for each test

1. `CreateAthleteClientAsync(...)`: a signed-in athlete (phone).
2. `SaveProfileAsync(athlete, "Athlete One")`: their profile, at Version 1.
3. `PairHeadsetAsync(athlete)`: a headset client that sends its token.

## Tests

| Test | What to check |
|---|---|
| `Headset_downloads_its_profile` | `GET /api/profile` → 200, right name, Version 1. In the database, the device's `LastSyncAt` is set. |
| `Higher_version_from_headset_is_saved` | Headset sends its copy with one change and Version 2 → 200. The phone's `GET /api/my-profile` shows the change and Version 2. |
| `Tie_or_lower_version_is_rejected_with_the_servers_copy` | The phone saves first (server now at Version 2). Headset sends Version 2 → 409, and Version 1 → 409. The 409 body is the server's copy (the phone's edit, Version 2). |
| `Invalid_profile_from_headset_is_rejected` | Exactly one bad value (`Brightness = 5`) with Version 2 → 400. `GET` still shows Version 1. |
| `Athlete_cookie_cannot_use_profile_sync` | The athlete's phone client calls `GET /api/profile` → 401. |

## Done when

`dotnet test` from `CompanionApp/Backend` shows **30 passed** (25 existing + these 5).
