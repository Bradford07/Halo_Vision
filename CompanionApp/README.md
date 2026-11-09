# Halo Vision Companion App

## Web app

The guide-facing web app lives in [`Web`](Web/README.md). It currently supports
account sign-in, editing the signed-in athlete's profile, and generating a
single-use headset pairing code.

Start the backend from `CompanionApp/Backend` with:

```powershell
dotnet run --project HaloVision.Api
```

Then follow [`Web/README.md`](Web/README.md) to start the front end.
