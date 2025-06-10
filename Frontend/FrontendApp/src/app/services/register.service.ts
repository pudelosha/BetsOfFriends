import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError, map, tap } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { RegisterResult } from '../model/register-result';
import { RegisterRequest } from '../model/register-request';

@Injectable({
  providedIn: 'root'
})
export class RegisterService {
  private apiUrl = `${environment.apiBaseUrl}/register`;

  constructor(private http: HttpClient) {}

  register(user: RegisterRequest): Observable<{ success: boolean; message: string; errors?: string[] }> {
    return this.http.post<RegisterResult>(`${this.apiUrl}/register`, user).pipe(
      tap((response) => console.log('Backend response:', response)),
      map((response) => ({
        success: response.success,
        message: response.message || 'Registration successful! Please check your email.',
        errors: response.errors ? response.errors.map(err => err.description) : undefined,
      })),
      catchError((error) => {
        console.error('Registration error:', error);
        return of({ success: false, message: 'An error occurred. Please try again.', errors: undefined });
      })
    );
  }

  resendActivationEmail(email: string): Observable<{ success: boolean; message: string }> {
    return this.http.post<{ success: boolean; message: string }>(`${this.apiUrl}/resend-confirmation`, { email }).pipe(
      map(response => ({
        success: response.success,
        message: response.message || "Activation email resent successfully."
      })),
      catchError((error) => {
        let errorMessage = "An error occurred. Please try again.";
  
        if (error.status === 400) {
          if (error.error && error.error.message) {
            errorMessage = error.error.message;
          } else {
            errorMessage = "Invalid email address or user not found.";
          }
        } else if (error.status === 403) {
          errorMessage = "This account is already confirmed.";
        } else if (error.status === 500) {
          errorMessage = "Server error. Please try again later.";
        }
  
        return of({ success: false, message: errorMessage });
      })
    );
  } 
  
  confirmEmail(userId: string, token: string): Observable<{ success: boolean; message: string }> {
    return this.http.get<{ success: boolean; message: string }>(`${this.apiUrl}/confirm-email`, {
      params: { userId, token },
    }).pipe(
      catchError(() => of({ success: false, message: "Unable to confirm email. Try again later." }))
    );
  }
  
  setupAccount(userId: string, token: string, password: string, language: string): Observable<{ success: boolean; message: string; errors?: string[] }> {
    const requestBody = { userId, token, password, language };
  
    return this.http.post<RegisterResult>(`${this.apiUrl}/setup-account`, requestBody).pipe(
      tap((response) => console.log('Backend response:', response)),
      map((response) => ({
        success: response.success,
        message: response.message || 'Account setup successful! You can now log in.',
        errors: response.errors ? response.errors.map(err => err.description) : undefined,
      })),
      catchError((error) => {
        console.error('Account setup error:', error);
        return of({ success: false, message: 'An error occurred while setting up the account. Please try again.', errors: undefined });
      })
    );
  }
}