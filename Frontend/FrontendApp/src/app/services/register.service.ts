import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError, map, tap } from 'rxjs/operators';
import { environment } from '../../environments/environment';

interface RegisterRequestDto {
  userName: string;
  email: string;
  password: string;
  consent: boolean;
}

interface RegisterResultDto {
  success: boolean;
  message?: string;
  errors?: { code: string; description: string }[];
}

@Injectable({
  providedIn: 'root'
})
export class RegisterService {
  private apiUrl = `${environment.apiBaseUrl}/register`;

  constructor(private http: HttpClient) {}

  register(user: RegisterRequestDto): Observable<{ success: boolean; message: string; errors?: string[] }> {
    return this.http.post<RegisterResultDto>(`${this.apiUrl}/register`, user).pipe(
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
}
