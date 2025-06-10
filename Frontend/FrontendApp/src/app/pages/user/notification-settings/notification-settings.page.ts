import { Component, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { ToastController, LoadingController } from '@ionic/angular';
import { TranslateModule } from '@ngx-translate/core';
import { TitleService } from 'src/app/services/title.service';
import { NotificationService } from 'src/app/services/notification.service';
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

  constructor(
    private fb: FormBuilder,
    private toastCtrl: ToastController,
    private loadingCtrl: LoadingController,
    private titleService: TitleService,
    private notificationService: NotificationService
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
    const loading = await this.loadingCtrl.create({ message: 'Loading settings...' });
    await loading.present();

    this.notificationService.getNotificationSettings().subscribe({
      next: async (settings) => {
        this.notificationForm.patchValue(settings);
        await loading.dismiss();
      },
      error: async () => {
        await loading.dismiss();
        this.showToast('Failed to load settings', 'danger');
      }
    });
  }

  async onSubmit() {
    if (!this.notificationForm.valid) return;

    const loading = await this.loadingCtrl.create({ message: 'Saving settings...' });
    await loading.present();

    this.notificationService.updateNotificationSettings(this.notificationForm.value).subscribe({
      next: async () => {
        await loading.dismiss();
        this.showToast('Notification settings saved.', 'success');
      },
      error: async () => {
        await loading.dismiss();
        this.showToast('Failed to save settings.', 'danger');
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
}