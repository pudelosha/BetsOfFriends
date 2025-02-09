import { Routes } from '@angular/router';
import { AuthGuard } from './guards/auth.guard';
import { GuestGuard } from './guards/guest.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'welcome', pathMatch: 'full' },

  // Only for logged-in users
  { path: 'home', loadComponent: () => import('./pages/home/home.page').then(m => m.HomePage), canActivate: [AuthGuard] },
  { path: 'profile', loadComponent: () => import('./pages/user/profile/profile.page').then( m => m.ProfilePage), canActivate: [AuthGuard] },
  { path: 'notification-settings', loadComponent: () => import('./pages/user/notification-settings/notification-settings.page').then( m => m.NotificationSettingsPage), canActivate: [AuthGuard] },
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
  { path: 'predefined-tournaments', loadComponent: () => import('./pages/tournaments/predefined-tournaments/predefined-tournaments.page').then( m => m.PredefinedTournamentsPage), canActivate: [AuthGuard] },
  { path: 'create-predefined-tournament', loadComponent: () => import('./pages/tournaments/create-predefined-tournament/create-predefined-tournament.page').then( m => m.CreatePredefinedTournamentPage), canActivate: [AuthGuard] },
  { path: 'update-predefined-tournament/:id', loadComponent: () => import('./pages/tournaments/create-predefined-tournament/create-predefined-tournament.page').then( m => m.CreatePredefinedTournamentPage), canActivate: [AuthGuard] },
  { path: 'my-tournaments', loadComponent: () => import('./pages/tournaments/my-tournaments/my-tournaments.page').then( m => m.MyTournamentsPage), canActivate: [AuthGuard] },


  // Only for guests
  { path: 'welcome', loadComponent: () => import('./pages/welcome/welcome.page').then(m => m.WelcomePage), canActivate: [GuestGuard] },
  { path: 'login', loadComponent: () => import('./pages/user/login/login.page').then(m => m.LoginPage), canActivate: [GuestGuard] },
  { path: 'register', loadComponent: () => import('./pages/user/register/register.page').then(m => m.RegisterPage), canActivate: [GuestGuard] },
  { path: 'resend-activation', loadComponent: () => import('./pages/user/resend-activation/resend-activation.page').then(m => m.ResendActivationPage), canActivate: [GuestGuard] },
  { path: 'forgot-password', loadComponent: () => import('./pages/user/forgot-password/forgot-password.page').then(m => m.ForgotPasswordPage), canActivate: [GuestGuard] },
  { path: 'reset-password', loadComponent: () => import('./pages/user/reset-password/reset-password.page').then(m => m.ResetPasswordPage), canActivate: [GuestGuard] },
  { path: 'logoff', loadComponent: () => import('./pages/user/logoff/logoff.page').then( m => m.LogoffPage), canActivate: [GuestGuard] },
  { path: 'confirm-email', loadComponent: () => import('./pages/user/confirm-email/confirm-email.page').then( m => m.ConfirmEmailPage ), canActivate: [GuestGuard] },

  { path: '**', redirectTo: 'welcome' },
 










];
