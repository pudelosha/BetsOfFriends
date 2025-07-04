
# DotNet-Ionic-Angular-AuthSystem

## Overview
This project is a **fun betting application**, where users can place bets (non-monetary, just for fun) on various events.

It consists of:  
- **Backend:** .NET 9 Web API with JWT-based authentication  
- **Frontend:** Ionic Angular v8 mobile-friendly client

### Features
- User registration and login
- JWT authentication & authorization
- Placing, viewing, and managing bets
- Email notifications
- Integration with Football Data API for real events

## Tech Stack
### Backend
- .NET 9
- ASP.NET Core Web API
- SQL Server
- Entity Framework Core
- JWT Authentication
- Email via SMTP

### Frontend
- Ionic Framework
- Angular
- `@ionic/angular` v8
- Capacitor/Cordova for mobile builds

## Getting Started

### Prerequisites
- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) & npm
- [Ionic CLI](https://ionicframework.com/docs/cli)  
  Install Ionic CLI:  
  ```bash
  npm install -g @ionic/cli
  ```
- SQL Server or Azure SQL instance

### Backend Setup
1. Navigate to the backend project folder:
   ```bash
   cd Backend
   ```
2. Configure connection strings and secrets in `appsettings.json` or environment variables.
3. Run migrations and update the database:
   ```bash
   dotnet ef database update
   ```
4. Run the API:
   ```bash
   dotnet run
   ```
   The API will be available at `https://localhost:7066` (or as configured).

### Frontend Setup
1. Navigate to the frontend project folder:
   ```bash
   cd Frontend
   ```
2. Install dependencies:
   ```bash
   npm install
   ```
3. Start the app:
   ```bash
   ionic serve
   ```
   The frontend will run at `http://localhost:8100`.

## Configuration
The backend uses a `appsettings.json` file for configuration, including:
- SQL Server connection strings
- JWT secret key, issuer, audience
- SMTP email credentials
- Football Data API token

⚠️ Make sure to replace placeholder values and never commit sensitive credentials to version control.

## Deployment
You can deploy the backend to Azure App Service or any cloud that supports .NET 9.  
The frontend can be deployed to Ionic Appflow, Firebase Hosting, or as a PWA.

## License
MIT License — see [LICENSE](LICENSE) for details.

## Authors
- [@pudelosha](https://github.com/pudelosha)
