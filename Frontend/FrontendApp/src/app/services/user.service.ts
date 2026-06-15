import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { UserProfile, ApplicationUser, PagedApplicationUsers } from '../model/user-profile';
import { ActionResult } from '../model/action-result';

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

  deleteAccount(password: string) {
    return this.http.post(`${this.apiUrl}/delete-account`, { password });
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

  getAllUsers(): Observable<ApplicationUser[]> {
    return this.http.get<ApplicationUser[]>(`${this.apiUrl}/all`);
  }

  getUsersPage(page = 1, pageSize = 20, search = ''): Observable<PagedApplicationUsers> {
    return this.http.get<PagedApplicationUsers>(`${this.apiUrl}/page`, {
      params: {
        page,
        pageSize,
        search
      }
    });
  }

  suspendUser(userId: string): Observable<ActionResult> {
    return this.http.post<ActionResult>(`${this.apiUrl}/suspend`, { userId });
  }
  
  unsuspendUser(userId: string): Observable<ActionResult> {
    return this.http.post<ActionResult>(`${this.apiUrl}/unsuspend`, { userId });
  }

  deleteUser(userId: string): Observable<ActionResult> {
    return this.http.post<ActionResult>(`${this.apiUrl}/delete`, { userId });
  }
}
