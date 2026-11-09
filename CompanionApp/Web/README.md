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

The repo includes GitHub Actions workflows for Azure Static Web Apps (the web
front end) and Azure App Service (the .NET API). They deploy when changes reach
`main`; the API workflow also runs the EF Core migrations at app startup.

### Azure resources

1. Create an Azure Static Web App on the **Standard** plan and an Azure App
   Service running **.NET 10** on Linux. Use the same region where possible.
2. For the Static Web App, set the GitHub deployment token as the repository
   Actions secret `AZURE_STATIC_WEB_APPS_API_TOKEN`.
3. For App Service, add repository variable `AZURE_WEBAPP_NAME` with the app's
   exact name, then add its downloaded publish profile as secret
   `AZURE_WEBAPP_PUBLISH_PROFILE`.
4. In App Service **Environment variables**, add the app setting
   `ConnectionStrings__Default` with value `Data Source=/home/halovision.db`.
   This keeps the SQLite database in App Service's persistent home storage;
   don't upload the local development database.
5. Push or merge the deployment files to `main`. GitHub Actions will publish
   the web app and API. After the first web deployment, open the Static Web App
   **APIs** page and link the App Service as its backend. The API already uses
   the `/api` route prefix, so the browser can keep calling `/api/...` on the
   public site origin.

The first Azure deployment starts with an empty database and creates its tables
from the existing migrations. Only Development receives the fake sample
athlete. The SQLite setup is intended for a small, single-instance project
demo; a larger public service should move to Azure SQL.

Azure Static Web Apps requires the Standard plan to link an App Service API.
Azure hosting plans can incur charges, so choose the plans under the project's
Azure subscription or student credits before creating the resources.
