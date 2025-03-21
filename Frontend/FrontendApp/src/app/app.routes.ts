import { Routes } from '@angular/router';
import { AuthGuard } from './guards/auth.guard';
import { GuestGuard } from './guards/guest.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'welcome', pathMatch: 'full' },

  // Only for logged-in users (Requires "User" role)
  { path: 'home', loadComponent: () => import('./pages/home/home/home.page').then(m => m.HomePage), canActivate: [AuthGuard], data: { role: 'User' } },
  { path: 'messages', loadComponent: () => import('./pages/messages/messages/messages.page').then( m => m.MessagesPage), canActivate: [AuthGuard], data: { role: 'User' } },
  { path: 'profile', loadComponent: () => import('./pages/user/profile/profile.page').then(m => m.ProfilePage), canActivate: [AuthGuard], data: { role: 'User' } },
  { path: 'notification-settings', loadComponent: () => import('./pages/user/notification-settings/notification-settings.page').then(m => m.NotificationSettingsPage), canActivate: [AuthGuard], data: { role: 'User' } },

  // My Bets (Requires "User" role)
  {
    path: 'my-bets',
    loadComponent: () => import('./pages/bets/my-bets/my-bets.page').then(m => m.MyBetsPage),
    canActivate: [AuthGuard],
    data: { role: 'User' },
  },

  // Tournaments (Requires "User" role)
  {
    path: 'tournaments/manage/stages',
    children: [
      { path: 'stage-input-type', loadComponent: () => import('./pages/tournaments/manage/stages/stage-input-type/stage-input-type.page').then(m => m.StageInputTypePage), canActivate: [AuthGuard], data: { role: 'User' } },
      { path: 'stage-teams-management', loadComponent: () => import('./pages/tournaments/manage/stages/stage-teams-management/stage-teams-management.page').then(m => m.StageTeamsManagementPage), canActivate: [AuthGuard], data: { role: 'User' } },
      { path: 'stage-stages-management', loadComponent: () => import('./pages/tournaments/manage/stages/stage-stages-management/stage-stages-management.page').then( m => m.StageStagesManagementPage), canActivate: [AuthGuard], data: { role: 'User' } },
      { path: 'stage-matches-management', loadComponent: () => import('./pages/tournaments/manage/stages/stage-matches-management/stage-matches-management.page').then(m => m.StageMatchesManagementPage), canActivate: [AuthGuard], data: { role: 'User' } },
      { path: 'stage-users-management', loadComponent: () => import('./pages/tournaments/manage/stages/stage-users-management/stage-users-management.page').then(m => m.StageUsersManagementPage), canActivate: [AuthGuard], data: { role: 'User' } },
      { path: 'stage-summary', loadComponent: () => import('./pages/tournaments/manage/stages/stage-summary/stage-summary.page').then(m => m.StageSummaryPage), canActivate: [AuthGuard], data: { role: 'User' } },
      { path: 'stage-settings', loadComponent: () => import('./pages/tournaments/manage/stages/stage-settings/stage-settings.page').then( m => m.StageSettingsPage), canActivate: [AuthGuard], data: { role: 'User' } },
    ]
  },

  // SuperAdmin-Only Routes
  {
    path: 'tournaments',
    children: [
      { 
        path: 'create-predefined', 
        loadComponent: () => import('./pages/tournaments/manage/predefined/build-predefined-tournament/build-predefined-tournament.page').then(m => m.BuildPredefinedTournamentPage), 
        canActivate: [AuthGuard], 
        data: { role: 'SuperAdmin' } 
      },
      { 
        path: 'update-predefined/:id', 
        loadComponent: () => import('./pages/tournaments/manage/predefined/build-predefined-tournament/build-predefined-tournament.page').then(m => m.BuildPredefinedTournamentPage), 
        canActivate: [AuthGuard], 
        data: { role: 'SuperAdmin' } 
      },
      { 
        path: 'predefined', 
        loadComponent: () => import('./pages/tournaments/manage/predefined/predefined-tournaments-list/predefined-tournaments-list.page').then(m => m.PredefinedTournamentsListPage), 
        canActivate: [AuthGuard], 
        data: { role: 'SuperAdmin' } 
      },
    ]
  },

  // User Tournament Routes (Requires "User" role)
  {
    path: 'tournaments',
    children: [
      { path: 'create-custom', loadComponent: () => import('./pages/tournaments/manage/custom/build-custom-tournament/build-custom-tournament.page').then(m => m.BuildCustomTournamentPage), canActivate: [AuthGuard], data: { role: 'User' } },
      { path: 'update-custom/:id', loadComponent: () => import('./pages/tournaments/manage/custom/build-custom-tournament/build-custom-tournament.page').then(m => m.BuildCustomTournamentPage), canActivate: [AuthGuard], data: { role: 'User' } },
      { path: 'custom', loadComponent: () => import('./pages/tournaments/manage/custom/custom-tournaments-list/custom-tournaments-list.page').then(m => m.CustomTournamentsListPage), canActivate: [AuthGuard], data: { role: 'User' } },
    ]
  },

  // Matches (Requires "User" role)
  {
    path: 'matches',
    loadComponent: () => import('./pages/matches/manage-matches/manage-matches.page').then(m => m.ManageMatchesPage),
    canActivate: [AuthGuard],
    data: { role: 'User' },
  },

  { path: 'my-tournaments', loadComponent: () => import('./pages/tournaments/my-tournaments-dashboard/my-tournaments-dashboard.page').then(m => m.MyTournamentsDashboardPage), canActivate: [AuthGuard], data: { role: 'User' } },
  { path: 'summary', loadComponent: () => import('./pages/tournaments/summary-dashboard/summary-dashboard.page').then(m => m.SummaryDashboardPage), canActivate: [AuthGuard], data: { role: 'User' } },
  { path: 'search', loadComponent: () => import('./pages/tournaments/search/search.page').then( m => m.SearchPage), canActivate: [AuthGuard], data: { role: 'User' } },

  // Only for guests
  { path: 'welcome', loadComponent: () => import('./pages/welcome/welcome.page').then(m => m.WelcomePage), canActivate: [GuestGuard] },
  { path: 'login', loadComponent: () => import('./pages/user/login/login.page').then(m => m.LoginPage), canActivate: [GuestGuard] },
  { path: 'register', loadComponent: () => import('./pages/user/register/register.page').then(m => m.RegisterPage), canActivate: [GuestGuard] },
  { path: 'resend-activation', loadComponent: () => import('./pages/user/resend-activation/resend-activation.page').then(m => m.ResendActivationPage), canActivate: [GuestGuard] },
  { path: 'forgot-password', loadComponent: () => import('./pages/user/forgot-password/forgot-password.page').then(m => m.ForgotPasswordPage), canActivate: [GuestGuard] },
  { path: 'reset-password', loadComponent: () => import('./pages/user/reset-password/reset-password.page').then(m => m.ResetPasswordPage), canActivate: [GuestGuard] },
  { path: 'logoff', loadComponent: () => import('./pages/user/logoff/logoff.page').then(m => m.LogoffPage), canActivate: [GuestGuard] },
  { path: 'confirm-email', loadComponent: () => import('./pages/user/confirm-email/confirm-email.page').then(m => m.ConfirmEmailPage), canActivate: [GuestGuard] },
  { path: 'setup-account',  loadComponent: () => import('./pages/user/setup-account/setup-account.page').then( m => m.SetupAccountPage), canActivate: [GuestGuard] },

  // Default and wildcard routes
  { path: '**', redirectTo: 'welcome' },

];
