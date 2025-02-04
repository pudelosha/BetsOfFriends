import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { UserProfile } from '../model/user-profile';

@Injectable({
  providedIn: 'root',
})
export class UserService {
  private apiUrl = `${environment.apiBaseUrl}/users`;

  constructor(private http: HttpClient) {}

  getUserProfile(): Observable<UserProfile> {
    console.log('attempting to execute get request to ' + `${this.apiUrl}/profile`);
    return this.http.get<UserProfile>(`${this.apiUrl}/profile`);
  }
  
  updateUserProfile(profile: Partial<UserProfile>): Observable<any> {
    return this.http.put(`${this.apiUrl}/profile`, profile);
  }

  changeEmail(newEmail: string, password: string) {
    return this.http.post(`${this.apiUrl}/change-email`, { newEmail, password });
  }

  updatePassword(currentPassword: string, newPassword: string) {
    return this.http.post(`${this.apiUrl}/update-password`, { currentPassword, newPassword });
  }
    
  forgotPassword(email: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/forgot-password`, { email });
  }

  resetPassword(userId: string, token: string, newPassword: string): Observable<{ success: boolean; message: string }> {
    return this.http.post<{ success: boolean; message: string }>(
      `${this.apiUrl}/reset-password`,
      { userId, token, newPassword }
    );
  }
}
