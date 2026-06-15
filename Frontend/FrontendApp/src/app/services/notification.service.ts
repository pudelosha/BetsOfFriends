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

export interface NotificationMessageRecipient {
  assignmentId: number;
  userName: string;
}

export interface SendTournamentUserMessagePayload {
  tournamentId: number;
  recipientAssignmentId: number;
  message: string;
}

export interface ReplyToUserMessagePayload {
  recipientUserId: string;
  tournamentId?: number | null;
  message: string;
}

export interface SendAdminBroadcastMessagePayload {
  message: string;
  sendEmail?: boolean;
}

export interface SendAdminUserMessagePayload {
  recipientUserId: string;
  message: string;
  sendEmail?: boolean;
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

  deleteAllNotifications(): Observable<{ deletedCount: number }> {
    return this.http.delete<{ deletedCount: number }>(`${this.apiUrl}/delete-all`);
  }

  getTournamentMessageRecipients(tournamentId: number): Observable<NotificationMessageRecipient[]> {
    return this.http.get<NotificationMessageRecipient[]>(`${this.apiUrl}/tournament-message-recipients/${tournamentId}`);
  }

  sendTournamentUserMessage(payload: SendTournamentUserMessagePayload): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/tournament-user-message`, payload);
  }

  replyToUserMessage(payload: ReplyToUserMessagePayload): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/reply-user-message`, payload);
  }

  sendAdminBroadcastMessage(payload: SendAdminBroadcastMessagePayload): Observable<{ message: string; recipientCount: number }> {
    return this.http.post<{ message: string; recipientCount: number }>(`${this.apiUrl}/admin-broadcast-message`, payload);
  }

  sendAdminUserMessage(payload: SendAdminUserMessagePayload): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/admin-user-message`, payload);
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
