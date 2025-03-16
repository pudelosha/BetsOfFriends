import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Notification as AppNotification } from 'src/app/model/notification';

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private apiUrl = `${environment.apiBaseUrl}/notifications`;

  constructor(private http: HttpClient) {}

  getLatestUserNotifications(): Observable<AppNotification[]> {
    return this.http.get<AppNotification[]>(`${this.apiUrl}/latest/`);
  }

  markNotificationAsRead(notificationId: number): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/read/${notificationId}`, {});
  }
}
