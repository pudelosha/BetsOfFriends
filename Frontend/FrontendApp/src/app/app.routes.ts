import { Routes } from '@angular/router';
import { AuthGuard } from './guards/auth.guard';
import { GuestGuard } from './guards/guest.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'welcome', pathMatch: 'full' },

  // Only for logged-in users
  { path: 'home', loadComponent: () => import('./pages/home/home.page').then(m => m.HomePage), canActivate: [AuthGuard] },

  // Only for guests
  { path: 'welcome', loadComponent: () => import('./pages/welcome/welcome.page').then(m => m.WelcomePage), canActivate: [GuestGuard] },
  { path: 'login', loadComponent: () => import('./pages/login/login.page').then(m => m.LoginPage), canActivate: [GuestGuard] },
  { path: 'register', loadComponent: () => import('./pages/register/register.page').then(m => m.RegisterPage), canActivate: [GuestGuard] },
  { path: 'resend-activation', loadComponent: () => import('./pages/resend-activation/resend-activation.page').then(m => m.ResendActivationPage), canActivate: [GuestGuard] },
  { path: 'forgot-password', loadComponent: () => import('./pages/forgot-password/forgot-password.page').then(m => m.ForgotPasswordPage), canActivate: [GuestGuard] },
  { path: 'reset-password', loadComponent: () => import('./pages/reset-password/reset-password.page').then(m => m.ResetPasswordPage), canActivate: [GuestGuard] },

  { path: '**', redirectTo: 'welcome' },
];
