import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject } from 'rxjs';
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
  private apiUrl = `${environment.apiBaseUrl}/authentication`; //
  private authTokenKey = 'authToken';
  private isAuthenticatedSubject = new BehaviorSubject<boolean>(this.isLoggedIn());

  constructor(private http: HttpClient) {}

  login(email: string, password: string): Observable<{ success: boolean; message: string }> {
    return this.http.post<LoginResponseDto>(`${this.apiUrl}/login`, { email, password }).pipe(
      map((response) => {
        if (response.success && response.token) {
          localStorage.setItem(this.authTokenKey, response.token);
          this.isAuthenticatedSubject.next(true);
          return { success: true, message: 'Login successful!' };
        }
        return { success: false, message: response.message || 'Login failed' };
      }),
      catchError(() => {
        return [{ success: false, message: 'An error occurred. Please try again.' }];
      })
    );
  }

  isLoggedIn(): boolean {
    return !!localStorage.getItem(this.authTokenKey);
  }

  logout(): void {
    localStorage.removeItem(this.authTokenKey);
    this.isAuthenticatedSubject.next(false);
  }

  getAuthStatus(): Observable<boolean> {
    return this.isAuthenticatedSubject.asObservable();
  }
}
