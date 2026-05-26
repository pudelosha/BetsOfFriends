import { Component, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { ToastController, LoadingController } from '@ionic/angular';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { TitleService } from 'src/app/services/title.service';
import { NotificationService, NotificationSettings } from 'src/app/services/notification.service';
import { PushNotificationService } from 'src/app/services/push-notification.service';
import { IonContent, IonGrid, IonRow, IonCol, IonToggle, IonButton } from '@ionic/angular/standalone';

@Component({
  selector: 'app-notification-settings',
  templateUrl: './notification-settings.page.html',
  styleUrls: ['./notification-settings.page.scss'],
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TranslateModule, IonContent, IonGrid, IonRow, IonCol, IonToggle, IonButton]
})
export class NotificationSettingsPage implements OnInit {
  notificationForm!: FormGroup;
  private readonly pushControlNames = [
    'receivePushMatchClosed',
    'receivePushDailyUpdates',
    'receivePushTournamentInvitation',
    'receivePushPendingBets',
    'receivePushNewGames',
    'receivePushSpecialOffers',
  ];

  constructor(
    private fb: FormBuilder,
    private toastCtrl: ToastController,
    private loadingCtrl: LoadingController,
    private titleService: TitleService,
    private notificationService: NotificationService,
    private pushNotificationService: PushNotificationService,
    private translate: TranslateService
  ) {}

  ngOnInit() {
    this.titleService.setTitle('NOTIFICATIONS.TITLE');

    this.notificationForm = this.fb.group({
      receiveEmailMatchClosed: [false],
      receivePushMatchClosed: [false],
      receiveEmailDailyUpdates: [false],
      receivePushDailyUpdates: [false],
      receiveEmailTournamentInvitation: [false],
      receivePushTournamentInvitation: [false],
      receiveEmailPendingBets: [false],
      receivePushPendingBets: [false],
      receiveEmailNewGames: [false],
      receivePushNewGames: [false],
      receiveEmailSpecialOffers: [false],
      receivePushSpecialOffers: [false],
    });

    this.loadSettings();
  }

  async loadSettings() {
    const loading = await this.loadingCtrl.create({ message: this.translate.instant('NOTIFICATIONS.LOADING_SETTINGS') });
    await loading.present();

    this.notificationService.getNotificationSettings().subscribe({
      next: async (settings) => {
        this.notificationForm.patchValue(settings);
        await loading.dismiss();
      },
      error: async () => {
        await loading.dismiss();
        this.showToastKey('NOTIFICATIONS.LOAD_FAILED', 'danger');
      }
    });
  }

  async onSubmit() {
    if (!this.notificationForm.valid) return;

    if (this.hasAnyPushEnabled()) {
      const pushResult = await this.pushNotificationService.ensureSubscription();

      if (!pushResult.success) {
        this.disablePushControls();
        await this.showToastKey(pushResult.messageKey ?? 'NOTIFICATIONS.PUSH_UNAVAILABLE', 'warning');
      }
    } else {
      try {
        await this.pushNotificationService.unsubscribeCurrentDevice();
      } catch {
        await this.showToastKey('NOTIFICATIONS.PUSH_REMOVE_FAILED', 'warning');
      }
    }

    const loading = await this.loadingCtrl.create({ message: this.translate.instant('NOTIFICATIONS.SAVING_SETTINGS') });
    await loading.present();

    this.notificationService.updateNotificationSettings(this.notificationForm.value as NotificationSettings).subscribe({
      next: async () => {
        await loading.dismiss();
        this.showToastKey('NOTIFICATIONS.SAVED', 'success');
      },
      error: async () => {
        await loading.dismiss();
        this.showToastKey('NOTIFICATIONS.SAVE_FAILED', 'danger');
      }
    });
  }

  async showToast(message: string, color: string) {
    const toast = await this.toastCtrl.create({
      message,
      duration: 3000,
      color,
      position: 'bottom',
    });
    await toast.present();
  }

  async showToastKey(key: string, color: string) {
    await this.showToast(this.translate.instant(key), color);
  }

  private hasAnyPushEnabled(): boolean {
    return this.pushControlNames.some(controlName => this.notificationForm.get(controlName)?.value === true);
  }

  private disablePushControls(): void {
    const patch = this.pushControlNames.reduce<Record<string, boolean>>((value, controlName) => {
      value[controlName] = false;
      return value;
    }, {});

    this.notificationForm.patchValue(patch);
  }
}
