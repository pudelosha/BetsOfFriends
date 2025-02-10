import { Routes } from '@angular/router';
import { AuthGuard } from './guards/auth.guard';
import { GuestGuard } from './guards/guest.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'welcome', pathMatch: 'full' },

  // Only for logged-in users
  { path: 'home', loadComponent: () => import('./pages/home/home.page').then(m => m.HomePage), canActivate: [AuthGuard] },
  { path: 'profile', loadComponent: () => import('./pages/user/profile/profile.page').then(m => m.ProfilePage), canActivate: [AuthGuard] },
  { path: 'notification-settings', loadComponent: () => import('./pages/user/notification-settings/notification-settings.page').then(m => m.NotificationSettingsPage), canActivate: [AuthGuard] },

  // My Bets
  {
    path: 'my-bets',
    loadComponent: () => import('./pages/bets/my-bets/my-bets.page').then(m => m.MyBetsPage),
    canActivate: [AuthGuard],
    children: [
      { path: 'to-place', loadComponent: () => import('./pages/bets/my-bets-to-place/my-bets-to-place.page').then(m => m.MyBetsToPlacePage), canActivate: [AuthGuard] },
      { path: 'placed', loadComponent: () => import('./pages/bets/my-bets-placed/my-bets-placed.page').then(m => m.MyBetsPlacedPage), canActivate: [AuthGuard] },
      { path: 'finalised', loadComponent: () => import('./pages/bets/my-bets-finalised/my-bets-finalised.page').then(m => m.MyBetsFinalisedPage), canActivate: [AuthGuard] },
      { path: '', redirectTo: 'to-place', pathMatch: 'full' }
    ]
  },
  { path: 'bet-preview', loadComponent: () => import('./pages/bets/bet-preview/bet-preview.page').then( m => m.BetPreviewPage), canActivate: [AuthGuard] },

  // Tournaments
  {
    path: 'tournaments/manage/stages',
    children: [
      { path: 'stage-input-type', loadComponent: () => import('./pages/tournaments/manage/stages/stage-input-type/stage-input-type.page').then(m => m.StageInputTypePage), canActivate: [AuthGuard] },
      { path: 'stage-teams-management', loadComponent: () => import('./pages/tournaments/manage/stages/stage-teams-management/stage-teams-management.page').then(m => m.StageTeamsManagementPage), canActivate: [AuthGuard] },
      { path: 'stage-matches-management', loadComponent: () => import('./pages/tournaments/manage/stages/stage-matches-management/stage-matches-management.page').then(m => m.StageMatchesManagementPage), canActivate: [AuthGuard] },
      { path: 'stage-users-management', loadComponent: () => import('./pages/tournaments/manage/stages/stage-users-management/stage-users-management.page').then(m => m.StageUsersManagementPage), canActivate: [AuthGuard] },
      { path: 'stage-summary', loadComponent: () => import('./pages/tournaments/manage/stages/stage-summary/stage-summary.page').then(m => m.StageSummaryPage), canActivate: [AuthGuard] },
    ]
  },

  {
    path: 'tournaments',
    children: [
      { path: 'create-predefined', loadComponent: () => import('./pages/tournaments/manage/predefined/build-predefined-tournament/build-predefined-tournament.page').then(m => m.BuildPredefinedTournamentPage), canActivate: [AuthGuard] },
      { path: 'update-predefined/:id', loadComponent: () => import('./pages/tournaments/manage/predefined/build-predefined-tournament/build-predefined-tournament.page').then(m => m.BuildPredefinedTournamentPage), canActivate: [AuthGuard] },
      { path: 'predefined', loadComponent: () => import('./pages/tournaments/manage/predefined/predefined-tournaments-list/predefined-tournaments-list.page').then(m => m.PredefinedTournamentsListPage), canActivate: [AuthGuard] },
    ]
  },

  {
    path: 'tournaments',
    children: [
      { path: 'create-custom', loadComponent: () => import('./pages/tournaments/manage/custom/build-custom-tournament/build-custom-tournament.page').then(m => m.BuildCustomTournamentPage), canActivate: [AuthGuard] },
      { path: 'update-custom/:id', loadComponent: () => import('./pages/tournaments/manage/custom/build-custom-tournament/build-custom-tournament.page').then(m => m.BuildCustomTournamentPage), canActivate: [AuthGuard] },
      { path: 'custom', loadComponent: () => import('./pages/tournaments/manage/custom/custom-tournaments-list/custom-tournaments-list.page').then(m => m.CustomTournamentsListPage), canActivate: [AuthGuard] },
    ]
  },

  { path: 'my-tournaments', loadComponent: () => import('./pages/tournaments/my-tournaments-dashboard/my-tournaments-dashboard.page').then(m => m.MyTournamentsDashboardPage), canActivate: [AuthGuard] },
  { path: 'summary', loadComponent: () => import('./pages/tournaments/summary-dashboard/summary-dashboard.page').then(m => m.SummaryDashboardPage), canActivate: [AuthGuard] },
  { path: 'live-results', loadComponent: () => import('./pages/tournaments/live-results-dashboard/live-results-dashboard.page').then(m => m.LiveResultsDashboardPage), canActivate: [AuthGuard] },

  // Only for guests
  { path: 'welcome', loadComponent: () => import('./pages/welcome/welcome.page').then(m => m.WelcomePage), canActivate: [GuestGuard] },
  { path: 'login', loadComponent: () => import('./pages/user/login/login.page').then(m => m.LoginPage), canActivate: [GuestGuard] },
  { path: 'register', loadComponent: () => import('./pages/user/register/register.page').then(m => m.RegisterPage), canActivate: [GuestGuard] },
  { path: 'resend-activation', loadComponent: () => import('./pages/user/resend-activation/resend-activation.page').then(m => m.ResendActivationPage), canActivate: [GuestGuard] },
  { path: 'forgot-password', loadComponent: () => import('./pages/user/forgot-password/forgot-password.page').then(m => m.ForgotPasswordPage), canActivate: [GuestGuard] },
  { path: 'reset-password', loadComponent: () => import('./pages/user/reset-password/reset-password.page').then(m => m.ResetPasswordPage), canActivate: [GuestGuard] },
  { path: 'logoff', loadComponent: () => import('./pages/user/logoff/logoff.page').then(m => m.LogoffPage), canActivate: [GuestGuard] },
  { path: 'confirm-email', loadComponent: () => import('./pages/user/confirm-email/confirm-email.page').then(m => m.ConfirmEmailPage), canActivate: [GuestGuard] },

  // Default and wildcard routes
  { path: '**', redirectTo: 'welcome' },


];
