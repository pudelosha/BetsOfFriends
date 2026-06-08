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
      map((response) => this.mapRegisterResult(response, 'Registration successful! Please check your email.')),
      catchError((error) => {
        console.error('Registration error:', error);
        return of(this.mapHttpError(error, 'An error occurred. Please try again.'));
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
      map(response => ({ success: response.success, message: response.message || 'Email confirmed.' })),
      catchError((error) => {
        const message = error && error.error && error.error.message ? error.error.message : 'Unable to confirm email. Try again later.';
        return of({ success: false, message });
      })
    );
  }
  
  setupAccount(userId: string, token: string, password: string, language: string): Observable<{ success: boolean; message: string; errors?: string[] }> {
    const requestBody = { userId, token, password, language };
  
    return this.http.post<RegisterResult>(`${this.apiUrl}/setup-account`, requestBody).pipe(
      tap((response) => console.log('Backend response:', response)),
      map((response) => this.mapRegisterResult(response, 'Account setup successful! You can now log in.')),
      catchError((error) => {
        console.error('Account setup error:', error);
        return of(this.mapHttpError(error, 'An error occurred while setting up the account. Please try again.'));
      })
    );
  }

  private mapRegisterResult(response: RegisterResult, fallbackMessage: string): { success: boolean; message: string; errors?: string[] } {
    const errors = this.extractErrors(response.errors);
    return {
      success: response.success,
      message: (!response.success ? errors?.join(' ') : undefined) || response.message || errors?.join(' ') || fallbackMessage,
      errors,
    };
  }

  private mapHttpError(error: any, fallbackMessage: string): { success: boolean; message: string; errors?: string[] } {
    const errors = this.extractErrors(error?.error?.errors);
    const message =
      errors?.join(' ') ||
      error?.error?.message ||
      error?.error?.Message ||
      error?.error?.title ||
      (error?.status === 0 ? 'Unable to reach the server. Please check your connection and try again.' : fallbackMessage);

    return { success: false, message, errors };
  }

  private extractErrors(errors: unknown): string[] | undefined {
    if (!errors) return undefined;

    if (Array.isArray(errors)) {
      const descriptions = errors
        .map(error => {
          if (typeof error === 'string') return error;
          if (error && typeof error === 'object') {
            const value = error as { description?: string; Description?: string; code?: string; Code?: string };
            return value.description || value.Description || value.code || value.Code;
          }
          return undefined;
        })
        .filter((error): error is string => !!error);

      return descriptions.length > 0 ? descriptions : undefined;
    }

    if (typeof errors === 'object') {
      const descriptions: string[] = [];

      Object.values(errors as Record<string, unknown>).forEach((value: unknown) => {
        const values = Array.isArray(value) ? value : [value];
        values.forEach((item: unknown) => {
          if (typeof item === 'string') {
            descriptions.push(item);
          }
        });
      });

      return descriptions.length > 0 ? descriptions : undefined;
    }

    return undefined;
  }
}
