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

## Publish to Azure

The repo includes a GitHub Actions workflow that builds the React front end,
places it in the .NET API's `wwwroot` folder, and deploys both together to one
Azure App Service. It runs when changes reach `main`; the API applies the EF
Core migrations at app startup.

### Azure setup

1. Create one **Linux Web App** using **.NET 10** and the **Free F1** plan.
   This tier is intended for development and testing and has limited daily
   compute and bandwidth, so the app can become unavailable after reaching its
   free quotas.
2. In GitHub repository **Settings → Secrets and variables → Actions**, add
   variable `AZURE_WEBAPP_NAME` with the app's exact name and secret
   `AZURE_WEBAPP_PUBLISH_PROFILE` with its downloaded publish profile.
3. In App Service **Environment variables**, add the app setting
   `ConnectionStrings__Default` with value `Data Source=/home/halovision.db`.
   On Linux App Service, `/home` is persistent app storage; don't upload the
   local development database.
4. Turn on **HTTPS Only** in App Service. The app serves the React front end
   and API from the same address, and sign-in cookies require HTTPS outside
   Development.
5. Once the App Service and GitHub settings are ready, merge the deployment
   workflow to `main`. GitHub Actions will build and deploy the web app and API
   together. The public address will be `https://<app-name>.azurewebsites.net`.

The first Azure deployment starts with an empty database and creates its tables
from the existing migrations. Only Development receives the fake sample
athlete. The SQLite setup is intended for a small, single-user project demo; a
larger public service should move to Azure SQL. Free F1 is useful for trying
the demo, but it has no SLA and may not be suitable for a dependable public
service.
