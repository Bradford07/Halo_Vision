# Halo Vision

**An augmented reality assistance system for low-vision athletes on paths and trails**

> **Before you push anything, read [CONTRIBUTING.md](CONTRIBUTING.md).**

## Overview

Halo Vision is a University of Nevada, Reno capstone project that uses augmented reality to help
low-vision athletes walk and jog on paths and trails with more independence. A Microsoft HoloLens 2
finds the path in each camera frame, draws its edges as a high-contrast outline, adds optional
spatial audio, and warns the athlete when it is unsure of the path.

The project began with adaptive skiing. Level 1 focuses on paved paths and dirt trails; snow and
skiing are planned for Level 2. Halo Vision is designed to **supplement trained human guides, not
replace them**.

## How it works

| Part | What it does |
| --- | --- |
| **Headset app** (HoloLens 2) | Draws the path outline and plays audio cues. Keeps the athlete's profile so it works on a trail with no network. |
| **Guide's web app** (phone browser) | The sighted guide signs in, creates athlete profiles, and pairs a headset. |
| **Backend** (server) | Stores guides, headsets and athlete profiles, and syncs profiles with the headset over Wi-Fi at the base. |
| **HaloVision.Shared** | C# classes, such as `AthleteProfile`, compiled by both the headset and the backend so they always agree on the data format. |

Our P3 design used a native phone app and Bluetooth. We replaced it with a web app and a backend,
because a web browser cannot host the Bluetooth service the headset connects to.

## Technology stack

| Part | Technologies |
| --- | --- |
| Headset app | Unity 2022.3.62f2, C#, OpenXR, UWP with IL2CPP, Visual Studio 2022 |
| Guide's web app | React, Vite, TypeScript (planned) |
| Backend | ASP.NET Core on .NET 10, EF Core with SQLite, ASP.NET Core Identity |
| Shared library | HaloVision.Shared: C# 9 and .NET Standard 2.1, packaged as a Unity local package |
| Tests | xUnit |
| Collaboration | Git and GitHub (see [CONTRIBUTING.md](CONTRIBUTING.md)) |

> **The Unity version is locked at 2022.3.62f2.** It is the only version that runs on our HoloLens 2.
> Do not upgrade it.

## Current status

**Done**
- Unity project configured for UWP and OpenXR, with a Hello World scene built, deployed, and launched
  on the HoloLens 2.

**In progress** (`backend-setup` branch)
- `HaloVision.Shared` with `AthleteProfile` and its enums, using the field names from our P3 design.
- ASP.NET Core backend with a `GET /health` endpoint.

**Planned**
- Backend: SQLite database, headset pairing, profile sync, test-log upload, and model info.
- Guide's web app: login, athlete list, profile editor, and pairing screens.
- Headset: camera feed, path segmentation, path outline, uncertainty warning, and spatial audio.
- Performance, latency, and usability testing.

## Repository structure

```text
Halo_Vision/
├── Headset/            # Unity project for the HoloLens 2 app
├── Shared/             # HaloVision.Shared: C# classes used by the headset and backend
├── CompanionApp/
│   └── Backend/        # ASP.NET Core backend (.NET 10)
├── CONTRIBUTING.md     # Team git rules: everyone must follow these
├── .gitignore
└── README.md
```

## Getting started

**Headset:** open `Headset/` in Unity 2022.3.62f2, then build for UWP and deploy with Visual Studio 2022.

**Backend:** install the .NET 10 SDK, then from `CompanionApp/Backend`:

```bash
dotnet build
dotnet run --project HaloVision.Api
```

Open `http://localhost:5233/health`. You should see `{"status":"ok"}`.

## Safety, privacy, and intended use

Halo Vision is an experimental accessibility project, not a certified navigation or safety system.

- A sighted spotter stays with the tester on every outdoor test.
- Level 1 live tests are walking and jogging only, starting on flat paved paths.
- Team members test first, before any low-vision athlete does.
- Before any test with visually impaired participants, the team confirms with the instructor whether
  university human-subjects review is required.
- Guidance builds never store camera frames. Test logs identify participants only by codes like
  "P03", never by name.

## Project team

**Team 14 OPCC:** Owen Cespon, Cal Peters, Connor James, Paris Acosta

**Instructors:** Erin Keith and Zach Estreito

**External advisor:** Monica Nicolescu, University of Nevada, Reno

## License

No license has been specified yet. Unless a license is added, the repository should not be assumed to
grant others permission to reuse or redistribute its contents.