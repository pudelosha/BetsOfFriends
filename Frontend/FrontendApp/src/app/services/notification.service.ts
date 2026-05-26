import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { NotificationDto } from 'src/app/model/notification';

export interface NotificationSettings {
  receiveEmailMatchClosed: boolean;
  receivePushMatchClosed: boolean;
  receiveEmailDailyUpdates: boolean;
  receivePushDailyUpdates: boolean;
  receiveEmailTournamentInvitation: boolean;
  receivePushTournamentInvitation: boolean;
  receiveEmailPendingBets: boolean;
  receivePushPendingBets: boolean;
  receiveEmailNewGames: boolean;
  receivePushNewGames: boolean;
  receiveEmailSpecialOffers: boolean;
  receivePushSpecialOffers: boolean;
}

export interface PushPublicKeyDto {
  enabled: boolean;
  publicKey?: string;
}

export interface PushSubscriptionPayload {
  endpoint: string;
  expirationTime: number | null;
  keys: {
    p256dh: string;
    auth: string;
  };
  userAgent: string;
}

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private apiUrl = `${environment.apiBaseUrl}/notifications`;

  constructor(private http: HttpClient) {}

  getLatestNotifications(): Observable<NotificationDto[]> {
    return this.http.get<NotificationDto[]>(`${this.apiUrl}/latest/`);
  }

  getNotifications(): Observable<NotificationDto[]> {
    return this.http.get<NotificationDto[]>(`${this.apiUrl}`);
  }

  markAsRead(notificationId: number): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/mark-as-read/${notificationId}`, {});
  }

  deleteNotification(notificationId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/delete/${notificationId}`);
  }

  getNotificationSettings(): Observable<NotificationSettings> {
    return this.http.get<NotificationSettings>(`${this.apiUrl}/settings`);
  }

  updateNotificationSettings(settings: NotificationSettings): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`${this.apiUrl}/settings`, settings);
  }

  getPushPublicKey(): Observable<PushPublicKeyDto> {
    return this.http.get<PushPublicKeyDto>(`${this.apiUrl}/push/public-key`);
  }

  savePushSubscription(subscription: PushSubscriptionPayload): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/push/subscriptions`, subscription);
  }

  deletePushSubscription(endpoint: string): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.apiUrl}/push/subscriptions`, {
      body: { endpoint }
    });
  }
}
