import { Routes } from '@angular/router';
import { AuthGuard } from './guards/auth.guard';
import { GuestGuard } from './guards/guest.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'welcome', pathMatch: 'full' },

  // Only for logged-in users
  { path: 'home', loadComponent: () => import('./pages/home/home.page').then(m => m.HomePage), canActivate: [AuthGuard] },
  { path: 'profile', loadComponent: () => import('./pages/profile/profile.page').then( m => m.ProfilePage), canActivate: [AuthGuard] },
  { path: 'notification-settings', loadComponent: () => import('./pages/notification-settings/notification-settings.page').then( m => m.NotificationSettingsPage), canActivate: [AuthGuard] },
  {
    path: 'my-bets',
    loadComponent: () => import('./pages/my-bets/my-bets.page').then(m => m.MyBetsPage),
    canActivate: [AuthGuard],
    children: [
      { path: 'to-place', loadComponent: () => import('./pages/my-bets-to-place/my-bets-to-place.page').then(m => m.MyBetsToPlacePage), canActivate: [AuthGuard] },
      { path: 'placed', loadComponent: () => import('./pages/my-bets-placed/my-bets-placed.page').then(m => m.MyBetsPlacedPage), canActivate: [AuthGuard] },
      { path: 'finalised', loadComponent: () => import('./pages/my-bets-finalised/my-bets-finalised.page').then(m => m.MyBetsFinalisedPage), canActivate: [AuthGuard] },
      { path: '', redirectTo: 'to-place', pathMatch: 'full' }
    ]
  },

  // Only for guests
  { path: 'welcome', loadComponent: () => import('./pages/welcome/welcome.page').then(m => m.WelcomePage), canActivate: [GuestGuard] },
  { path: 'login', loadComponent: () => import('./pages/login/login.page').then(m => m.LoginPage), canActivate: [GuestGuard] },
  { path: 'register', loadComponent: () => import('./pages/register/register.page').then(m => m.RegisterPage), canActivate: [GuestGuard] },
  { path: 'resend-activation', loadComponent: () => import('./pages/resend-activation/resend-activation.page').then(m => m.ResendActivationPage), canActivate: [GuestGuard] },
  { path: 'forgot-password', loadComponent: () => import('./pages/forgot-password/forgot-password.page').then(m => m.ForgotPasswordPage), canActivate: [GuestGuard] },
  { path: 'reset-password', loadComponent: () => import('./pages/reset-password/reset-password.page').then(m => m.ResetPasswordPage), canActivate: [GuestGuard] },
  { path: 'logoff', loadComponent: () => import('./pages/logoff/logoff.page').then( m => m.LogoffPage), canActivate: [GuestGuard] },
  { path: 'confirm-email', loadComponent: () => import('./pages/confirm-email/confirm-email.page').then( m => m.ConfirmEmailPage ), canActivate: [GuestGuard] },

  { path: '**', redirectTo: 'welcome' },
  {
    path: 'my-bets',
    loadComponent: () => import('./pages/my-bets/my-bets.page').then( m => m.MyBetsPage)
  },
  {
    path: 'my-bets-to-place',
    loadComponent: () => import('./pages/my-bets-to-place/my-bets-to-place.page').then( m => m.MyBetsToPlacePage)
  },
  {
    path: 'my-bets-placed',
    loadComponent: () => import('./pages/my-bets-placed/my-bets-placed.page').then( m => m.MyBetsPlacedPage)
  },
  {
    path: 'my-bets-finalised',
    loadComponent: () => import('./pages/my-bets-finalised/my-bets-finalised.page').then( m => m.MyBetsFinalisedPage)
  },




];
