import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject, throwError } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment';

interface LoginResponseDto {
  success: boolean;
  token?: string;
  message?: string;
  errors?: string[];
}

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private apiUrl = `${environment.apiBaseUrl}/authentication`;
  private authTokenKey = 'authToken';
  private isAuthenticatedSubject = new BehaviorSubject<boolean>(this.isLoggedIn());

  constructor(private http: HttpClient) {}

  login(email: string, password: string): Observable<{ success: boolean; message: string }> {
    return this.http.post<LoginResponseDto>(`${this.apiUrl}/login`, { email, password }).pipe(
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
  
  isLoggedIn(): boolean {
    return !!localStorage.getItem(this.authTokenKey);
  }

  logout(): void {
    console.log('Clearing auth token...');
    localStorage.removeItem(this.authTokenKey);
    console.log('Updating authentication state...');
    this.isAuthenticatedSubject.next(false);
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
