# Backend tests to write

Two branches, one new test file on each. Every file path below starts in `CompanionApp/Backend/`.

**Done when:** on each branch, `dotnet test` from `CompanionApp/Backend` passes, with the new tests added to the 25 existing ones.

## Helpers you'll use (both branches)

In `HaloVision.Api.Tests/TestAppFactory.cs`:
- `CreateAthleteClientAsync(email)`: a signed-in athlete (the phone)
- `SaveProfileAsync(athlete, name)`: their profile, at Version 1
- `PairHeadsetAsync(athlete)`: a headset client that sends its token
- `Json`: the JSON settings the API uses
- `GetDb(scope)`: direct database access, for checking results

`PairingTests.cs` and `DeviceAuthTests.cs` are examples of the same style.

**Setup for every test:** athlete → `SaveProfileAsync` → `PairHeadsetAsync`.

---

## 1. Profile sync: new branch `feature/profile-sync-tests` from main

The sync code is already in main (PR #12 is merged), so make a new branch from main for these tests.

**New file:** `HaloVision.Api.Tests/ProfileSyncTests.cs`
**Code being tested:** `HaloVision.Api/Features/Sync/ProfileSyncEndpoints.cs` (`GET /api/profile`, `PUT /api/profile`)
**Also read:** `HaloVision.Api/Features/Athletes/AthleteProfileRules.cs` (the value checks `PUT` uses)

**Sync rule:** the server saves the headset's copy only if its `Version` is **higher** than the server's.
A tie or a lower number gets **409**, and the response body is the server's copy.

| Test | What to check |
|---|---|
| `Headset_downloads_its_profile` | `GET /api/profile` → 200, right name, Version 1. In the database, the device's `LastSyncAt` is set. |
| `Higher_version_from_headset_is_saved` | Headset sends its copy with one change and Version 2 → 200. The phone's `GET /api/my-profile` shows the change and Version 2. |
| `Tie_or_lower_version_is_rejected_with_the_servers_copy` | The phone saves first (server now at Version 2). Headset sends Version 2 → 409, and Version 1 → 409. The 409 body is the server's copy (the phone's edit, Version 2). |
| `Invalid_profile_from_headset_is_rejected` | Exactly one bad value (`Brightness = 5`) with Version 2 → 400. `GET` still shows Version 1. |
| `Athlete_cookie_cannot_use_profile_sync` | The athlete's phone client calls `GET /api/profile` → 401. |

**Expected:** 30 passed (25 + 5).

---

## 2. Test logs: branch `feature/test-logs`

**New file:** `HaloVision.Api.Tests/TestLogTests.cs`
**Code being tested:** `HaloVision.Api/Features/TestLogs/TestLogEndpoints.cs` (`POST /api/test-logs`)
**Also read:** `Shared/Runtime/TestLog.cs` (repo root, not `CompanionApp/Backend`): the log class. Its `Id` is a `Guid` the headset makes.

**Rule:** the body is a list of logs. Logs the server already has (same `Id`) are skipped, so sending a batch twice is safe.
The answer is `{ "accepted": [ids] }`: every id the server now has, whether it was saved now or on an earlier try.
`DeviceId` always comes from the headset's token, never from the body.

| Test | What to check |
|---|---|
| `Same_batch_twice_stores_each_log_once` | **(P4 required)** Make 2 logs with `Guid.NewGuid()` ids. Send them, then send the same 2 again. Both answers are 200 and list both ids as accepted. The database has exactly 2 rows in `TestLogs`. |
| `Device_id_comes_from_the_token` | Send a log with `DeviceId = 999`. The stored row has the real headset's device id (from `GET /api/devices/me` or the database). |
| `Log_without_an_id_is_not_accepted` | Send a log with `Id = Guid.Empty`. It's not in `accepted`, and nothing is stored. |
| `Athlete_cookie_cannot_upload_logs` | The athlete's phone client posts to `/api/test-logs` → 401. |

**Expected:** 29 passed (25 + 4).

Once both sets of tests are merged into main, the total is 34.
