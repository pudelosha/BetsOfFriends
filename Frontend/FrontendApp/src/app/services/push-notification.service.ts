import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { NotificationService, PushSubscriptionPayload } from './notification.service';

export interface PushRegistrationResult {
  success: boolean;
  message?: string;
}

@Injectable({
  providedIn: 'root'
})
export class PushNotificationService {
  constructor(private notificationService: NotificationService) {}

  get isSupported(): boolean {
    return typeof window !== 'undefined' &&
      'serviceWorker' in navigator &&
      'PushManager' in window &&
      'Notification' in window;
  }

  async ensureSubscription(): Promise<PushRegistrationResult> {
    if (!this.isSupported) {
      return {
        success: false,
        message: 'Push notifications are not supported in this browser.'
      };
    }

    const pushConfig = await firstValueFrom(this.notificationService.getPushPublicKey());
    if (!pushConfig.enabled || !pushConfig.publicKey) {
      return {
        success: false,
        message: 'Push notifications are not configured yet.'
      };
    }

    const permission = await Notification.requestPermission();
    if (permission !== 'granted') {
      return {
        success: false,
        message: 'Push notification permission was not granted.'
      };
    }

    const registration = await navigator.serviceWorker.register('/push-sw.js');
    let subscription = await registration.pushManager.getSubscription();

    if (!subscription) {
      subscription = await registration.pushManager.subscribe({
        userVisibleOnly: true,
        applicationServerKey: this.urlBase64ToUint8Array(pushConfig.publicKey)
      });
    }

    await firstValueFrom(this.notificationService.savePushSubscription(this.toPayload(subscription)));

    return { success: true };
  }

  async unsubscribeCurrentDevice(): Promise<void> {
    if (!this.isSupported) {
      return;
    }

    const registration = await navigator.serviceWorker.getRegistration();
    const subscription = await registration?.pushManager.getSubscription();

    if (!subscription) {
      return;
    }

    await firstValueFrom(this.notificationService.deletePushSubscription(subscription.endpoint));
    await subscription.unsubscribe();
  }

  private toPayload(subscription: PushSubscription): PushSubscriptionPayload {
    const json = subscription.toJSON() as PushSubscriptionJSON & {
      keys?: {
        p256dh?: string;
        auth?: string;
      };
    };

    if (!json.keys?.p256dh || !json.keys.auth) {
      throw new Error('Push subscription keys are missing.');
    }

    return {
      endpoint: subscription.endpoint,
      expirationTime: subscription.expirationTime,
      keys: {
        p256dh: json.keys.p256dh,
        auth: json.keys.auth
      },
      userAgent: navigator.userAgent
    };
  }

  private urlBase64ToUint8Array(base64String: string): Uint8Array {
    const padding = '='.repeat((4 - base64String.length % 4) % 4);
    const base64 = `${base64String}${padding}`
      .replace(/-/g, '+')
      .replace(/_/g, '/');
    const rawData = window.atob(base64);
    const outputArray = new Uint8Array(rawData.length);

    for (let i = 0; i < rawData.length; i += 1) {
      outputArray[i] = rawData.charCodeAt(i);
    }

    return outputArray;
  }
}
