# News Analyzer - Local Setup Guide

Follow these steps to run the News Analyzer full-stack application on a new device.

## Prerequisites
Before you begin, ensure you have the following installed on your machine:
- **Node.js** (v18+ recommended) & **npm**
- **Angular CLI** (`npm install -g @angular/cli`)
- **.NET 9.0 SDK** (or the version specified in your project)
- **SQL Server** (LocalDB or an accessible SQL instance)

---

## 1. Clone the Repository
Clone the repository to your local machine:
```bash
git clone https://github.com/Arjunwhatever/News_Analyzer-.git
cd News_Analyzer-
```

---

## 2. Setting up the Backend (ASP.NET Core)

1. Navigate to the backend directory:
   ```bash
   cd backend
   ```
2. Restore the required .NET packages:
   ```bash
   dotnet restore
   ```
3. **Configure Secrets/API Keys**:
   The application requires several API keys to function (Google/Gemini API for analysis, NewsAPI for the live feed). 
   You must create an `appsettings.secrets.json` file in the `backend/` folder (or use .NET User Secrets) with your valid API keys and connection strings:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=NewsAnalyzerDB;Trusted_Connection=True;MultipleActiveResultSets=true"
     },
     "AppSettings": {
       "GoogleApiKey": "YOUR_GOOGLE_GEMINI_API_KEY",
       "NewsApiKey": "YOUR_NEWS_API_KEY",
       "OpenRouterApiKey": "YOUR_OPENROUTER_API_KEY"
     }
   }
   ```
4. **Apply Database Migrations**:
   Run Entity Framework tools to create your local SQL database schema:
   ```bash
   dotnet ef database update
   ```
   *(Note for Windows users: The standard .NET SDK includes **LocalDB** out of the box. Running this command automatically spins up the database in the background, so you do not need to install or start a separate SQL Server manually)*
5. **Run the Backend Server**:
   You can run the server using the standard command:
   ```bash
   dotnet run
   ```
   *(Alternatively, use `dotnet run --launch-process http` to ensure it explicitly binds to the HTTP profile).*
   The backend should now be running (typically on `http://localhost:5131`).

---

## Running on Mac / Linux (Important Database Step)
Entity Framework uses SQL Server LocalDB by default, which is **Windows-only**. If you are on a Mac or Linux, you must run SQL Server via Docker and update your connection string.

1. **Start SQL Server in Docker**:
   ```bash
   docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=YourStrong!Passw0rd" -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest
   ```
2. **Update your Connection String** (in `appsettings.secrets.json`):
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=localhost,1433;Database=NewsAnalyzerDB;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;MultipleActiveResultSets=true"
   }
   ```
3. Then proceed with `dotnet ef database update` and `dotnet run` as normal.

---

## 3. Setting up the Frontend (Angular)

1. Open a new terminal window and navigate to the frontend directory:
   ```bash
   cd frontend
   ```
2. Install the Node dependencies:
   ```bash
   npm install
   ```
3. **Run the Development Server**:
   ```bash
   ng serve
   ```
   *Alternatively, if using SSL in development, you can use `npm start` depending on your `package.json` configuration.*

4. **Access the App**:
   Open your browser and navigate to `http://localhost:4200/` (or the port specified by your CLI). The Angular proxy is configured to automatically route `/api` requests to your local .NET backend.

---
