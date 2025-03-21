import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonicModule, ToastController } from '@ionic/angular';
import { ReactiveFormsModule, FormBuilder, FormGroup } from '@angular/forms';

@Component({
  selector: 'app-notification-settings',
  templateUrl: './notification-settings.page.html',
  styleUrls: ['./notification-settings.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule],
})
export class NotificationSettingsPage implements OnInit {
  notificationForm: FormGroup;

  constructor(private fb: FormBuilder, private toastCtrl: ToastController) {
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
      receivePushSpecialOffers: [false]
    });
  }

  ngOnInit() {}

  async onSubmit() {
    console.log('Updated Notification Settings:', this.notificationForm.value);
    const toast = await this.toastCtrl.create({
      message: 'Notification settings saved.',
      duration: 3000,
      color: 'success',
      position: 'bottom'
    });
    await toast.present();
  }
}
