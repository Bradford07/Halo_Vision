# Halo Vision web app

A small React and Vite front end for the current companion API. It supports:

- Athlete account registration and sign-in
- Viewing and editing the signed-in account's profile
- Creating a 10-minute headset pairing code

The backend currently stores one athlete profile per account. This first web
screen follows that API contract.

## Run locally

Use Node.js 20.19+ (or 22.12+) and npm.

1. Start the API from `CompanionApp/Backend`:

   ```powershell
   dotnet run --project HaloVision.Api
   ```

   The API listens at `http://localhost:5233` in its development profile.

2. In another terminal, from `CompanionApp/Web`, install dependencies and start
   Vite:

   ```powershell
   npm install
   npm run dev
   ```

3. Open the local URL printed by Vite. Its `/api` proxy sends requests to the
   backend, keeping the Identity sign-in cookie on the same browser origin.

The headset redeems the displayed pairing code through `POST /api/pairing/claim`.
