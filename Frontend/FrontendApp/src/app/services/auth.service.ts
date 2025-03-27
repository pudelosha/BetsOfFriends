import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject, throwError } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { ToastController } from '@ionic/angular';
import { LoginResponse } from '..//model/login-response'
import { Router } from '@angular/router';
import { jwtDecode } from 'jwt-decode';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private apiUrl = `${environment.apiBaseUrl}/authentication`;
  private authTokenKey = 'authToken';
  private storageKey = 'selectedTournamentId';
  private isAuthenticatedSubject = new BehaviorSubject<boolean>(this.isLoggedIn());

  constructor(private http: HttpClient, private toastCtrl: ToastController, private router: Router) {}

  login(email: string, password: string): Observable<{ success: boolean; message: string }> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/login`, { email, password }).pipe(
      map((response) => {
        console.log('Backend response:', response);
        if (response.success && response.token) {
          localStorage.setItem(this.authTokenKey, response.token);
          this.isAuthenticatedSubject.next(true);
          return { success: true, message: 'Login successful!' };
        }
        return { success: false, message: response.message || 'Login failed' };
      }),
      catchError((error) => {
        console.error('Login error:', error);
        return throwError(() => this.getErrorMessage(error));
      })
    );
  }

  getToken(): string | null {
    return localStorage.getItem(this.authTokenKey);
  }
  
  isLoggedIn(): boolean {
    return !!localStorage.getItem(this.authTokenKey);
  }

  getUserRoles(): string[] {
    const token = this.getToken();
    if (token) {
        try {
            const decodedToken: any = jwtDecode(token);
            console.log("Decoded Token:", decodedToken); // Debugging step

            // Extract roles correctly from claim URL
            const roleClaim = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
            return decodedToken[roleClaim] 
                ? Array.isArray(decodedToken[roleClaim])
                    ? decodedToken[roleClaim]
                    : [decodedToken[roleClaim]] 
                : [];
        } catch (error) {
            console.error("Error decoding token:", error);
            return [];
        }
    }
    return [];
  }

  async logout(message?: string, redirectPath: string = '/welcome'): Promise<void> {
    console.log('Clearing auth tokens...');
    localStorage.removeItem(this.authTokenKey);
    localStorage.removeItem(this.storageKey);
    sessionStorage.removeItem(this.authTokenKey);
    console.log('Updating authentication state...');
    this.isAuthenticatedSubject.next(false);
  
    if (message) {
      const toast = await this.toastCtrl.create({
        message,
        duration: 3000,
        color: 'success',
        position: 'bottom',
      });
      await toast.present();
    }
  
    console.log(`Redirecting to ${redirectPath}...`);
    setTimeout(() => {
      this.router.navigate([redirectPath]);
    }, 3000);
  }
    
  getAuthStatus(): Observable<boolean> {
    return this.isAuthenticatedSubject.asObservable();
  }

  private getErrorMessage(error: any): { success: boolean; message: string } {
    if (error.status === 400 && error.error?.errors) {
      return { success: false, message: this.formatValidationErrors(error.error.errors) };
    }
  
    if (error.error?.message) {
      return { success: false, message: error.error.message };
    }
  
    switch (error.status) {
      case 401:
        return { success: false, message: 'Invalid email or password. Please try again.' };
      case 403:
        return { success: false, message: 'Your account is locked. Contact support.' };
      case 404:
        return { success: false, message: 'User not found. Please register first.' };
      case 500:
        return { success: false, message: 'Server error. Please try again later.' };
      default:
        return { success: false, message: 'An unexpected error occurred. Please try again.' };
    }
  }  

  private formatValidationErrors(errors: Record<string, string[]>): string {
    return Object.values(errors)
      .map((messages) => messages.join(', '))
      .join(' ');
  }
}
