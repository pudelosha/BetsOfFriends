# BetsofFriends — Fun Betting App

BetsofFriends is a **fun, social betting application** where users can create or join tournaments, place bets on real matches, and compete with friends — all without using real money.

Live at 👉 **[betsoffriends.com](https://betsoffriends.com)**

---

## 🎯 Overview

This project is built with a **.NET 9 backend** and an **Ionic Angular v8 frontend**, designed to deliver a smooth, mobile-friendly betting experience.  
The app integrates with **Football Data APIs** to pull real-world match results and updates, processed automatically by a **.NET Hosted Service** running in the background.

### Key Features

- 👥 User registration and login with JWT authentication  
- 🏆 Create and manage custom tournaments with friends  
- ⚽ Integration with real football events via external APIs  
- 💬 Messaging and notifications  
- 📊 Bet tracking, live match insights, and results view  
- 🧩 Role-based access levels: User, Admin, Super Admin  
- 🔔 Email notifications via SMTP  
- 🌍 Mobile-ready (Ionic Angular)

---

## 🧱 Architecture

| Layer | Technology |
|-------|-------------|
| **Backend** | .NET 9 Web API |
| **Frontend** | Ionic Angular v8 |
| **Database** | SQL Server |
| **ORM** | Entity Framework Core |
| **Auth** | JWT Authentication |
| **Background Jobs** | Hosted Service (match result sync) |
| **Emails** | SMTP-based notifications |

---

## 🧩 Roles & Permissions

### 👤 Regular User
- Create and join tournaments  
- Place bets on matches  
- View results, match insights, and statistics  

### ⚙️ Admin
- Manage tournaments, matches, and participants  
- Approve or moderate community tournaments  

### 🌟 Super Admin
- Create predefined tournaments (e.g., *FIFA World Cup 2026*)  
- Manage users and predefined events  

---

## 🧭 Navigation Overview

The main sidebar (as seen in the screenshot) includes:

- **Home** – Dashboard overview  
- **Messages** – Communication center  
- **Tournaments**
  - *My Tournaments*
  - *Find Tournament*
- **Betting**
  - *My Bets*
  - *Match Insights*
  - *Results*
- **Admin**
  - *Create Tournament*
  - *Manage Tournaments*
  - *Manage Matches*
  - *Manage Participants*
- **Super Admin**
  - *Create Predefined Tournament*
  - *Manage Predefined Tournaments*
  - *Manage Users*
- **Settings**
  - *Profile*, *Notifications*, *Info & Support*, *Logout*

---

## ⚙️ Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) & npm
- [Ionic CLI](https://ionicframework.com/docs/cli)
  ```bash
  npm install -g @ionic/cli
  ```
- SQL Server (local or Azure)

---

## 🚀 Backend Setup

1. Navigate to the backend folder:
   ```bash
   cd Backend
   ```

2. Configure environment variables in `appsettings.json` or `.env`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Database=BetsofFriends;User Id=appuser;Password=apppass;TrustServerCertificate=True;"
     },
     "Jwt": {
       "Key": "your-secret-key",
       "Issuer": "BetsofFriendsAPI",
       "Audience": "BetsofFriendsClient"
     },
     "Smtp": {
       "Host": "smtp.example.com",
       "Port": 587,
       "User": "noreply@example.com",
       "Password": "yourpassword"
     }
   }
   ```

3. Apply migrations and seed the database:
   ```bash
   dotnet ef database update
   ```

4. Run the API:
   ```bash
   dotnet run
   ```
   Available at: **https://localhost:7066**

---

## 📱 Frontend Setup

1. Navigate to the frontend:
   ```bash
   cd Frontend
   ```

2. Install dependencies:
   ```bash
   npm install
   ```

3. Start the app in development:
   ```bash
   ionic serve
   ```
   Runs at **http://localhost:8100**

---

## 🔄 Hosted Service (Live Match Sync)

A background **Hosted Service** runs automatically in the backend to:
- Fetch live football results from the external API  
- Update tournament and match outcomes  
- Recalculate user bet results and leaderboard standings in real time  

---

## ⚙️ Configuration Summary

| Setting | Description |
|----------|-------------|
| **SQL Server** | Connection string for persistent storage |
| **JWT** | Authentication & authorization secrets |
| **SMTP** | Email sending configuration |
| **Football Data API Token** | Used by Hosted Service to fetch real results |

⚠️ **Never commit secrets** to version control — use environment variables or secret managers.

---

## 🧩 Deployment

### Backend
- Deployable to **Azure App Service**, **Docker**, or any .NET-compatible host.
- Includes Hosted Service for automated result updates.

### Frontend
- Deployable to **Ionic Appflow**, **Firebase Hosting**, or **PWA** build.

---

## 🧠 Roadmap

- 🏅 Enhanced leaderboards & global rankings  
- 📊 Match history and performance analytics  
- 🔔 Push notifications (FCM or OneSignal)  
- 🤝 Team-based tournaments and achievements  
- 📱 Full mobile packaging via Capacitor  

---

## 📸 Screenshots

| | | |
|---|---|---|
| ![1](https://github.com/user-attachments/assets/6f8bf071-be76-4c67-a12e-dd908f4fcb02) | ![2](https://github.com/user-attachments/assets/6bdf198f-c0cd-4bae-bdc9-3003805daa96) | ![3](https://github.com/user-attachments/assets/d10c298c-4767-493e-a000-b25c474a21b4) |
| ![4](https://github.com/user-attachments/assets/6b150d95-e0ae-4dcd-86d7-cfc617ed0f6f) | ![5](https://github.com/user-attachments/assets/d1d7c834-5fe4-4239-b86b-04d93c42dd3c) | ![6](https://github.com/user-attachments/assets/714df2bd-3127-41cf-a187-d47b2b26538a) |


