import { Injectable } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, RouterStateSnapshot, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { ToastController } from '@ionic/angular';

@Injectable({
  providedIn: 'root',
})
export class AuthGuard implements CanActivate {
  constructor(
    private authService: AuthService, 
    private router: Router, 
    private toastCtrl: ToastController
  ) {}

  async canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Promise<boolean> {
    console.log("AuthGuard: Checking user roles...");

    // Check if user is logged in
    const isLoggedIn = this.authService.isLoggedIn();
    if (!isLoggedIn) {
      console.warn("AuthGuard: User is not logged in. Redirecting to welcome.");
      this.showToast("You must be logged in to access this page.");
      this.router.navigate(['/welcome']);
      return false;
    }

    // Get user roles (ensure it's always an array)
    const userRoles = this.authService.getUserRoles() || [];
    console.log("AuthGuard: User roles:", userRoles);

    // If there are no roles, handle error scenario
    if (userRoles.length === 0) {
      console.error("AuthGuard: User has no assigned role. Redirecting to home.");
      this.showToast("Your account has no assigned role. Contact support.");
      this.router.navigate(['/home']);
      return false;
    }

    // Get required role from route metadata
    const requiredRole = route.data['role'];
    console.log("AuthGuard: Required role:", requiredRole);

    // If no specific role is required, allow access
    if (!requiredRole) {
      return true;
    }

    // Role hierarchy: Higher roles can access lower roles
    const roleHierarchy: { [key: string]: string[] } = {
      "SuperAdmin": ["SuperAdmin", "Admin", "User"],
      "Admin": ["Admin", "User"],
      "User": ["User"]
    };

    // Check if user has required role or a higher one
    const hasAccess = userRoles.some(role => roleHierarchy[role]?.includes(requiredRole));

    if (hasAccess) {
      console.log(`AuthGuard: Access granted for required role: ${requiredRole}`);
      return true;
    }

    // If access is denied, redirect & show a toast
    console.warn(`AuthGuard: Access denied. User roles: ${userRoles}, Required role: ${requiredRole}`);
    this.showToast("You don't have permission to access this page.");
    this.router.navigate(['/home']);
    return false;
  }

  private async showToast(message: string) {
    const toast = await this.toastCtrl.create({
      message,
      duration: 3000,
      color: 'danger',
      position: 'bottom',
    });
    await toast.present();
  }
}
