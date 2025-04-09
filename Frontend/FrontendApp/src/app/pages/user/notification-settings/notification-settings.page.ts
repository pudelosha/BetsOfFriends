import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonicModule, ToastController } from '@ionic/angular';
import { ReactiveFormsModule, FormBuilder, FormGroup } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { TitleService } from 'src/app/services/title.service';


@Component({
  selector: 'app-notification-settings',
  templateUrl: './notification-settings.page.html',
  styleUrls: ['./notification-settings.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule, TranslateModule],
})
export class NotificationSettingsPage implements OnInit {
  notificationForm: FormGroup;

  constructor(private fb: FormBuilder, private toastCtrl: ToastController, private titleService: TitleService) {
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

  ngOnInit() {
    this.titleService.setTitle('NOTIFICATIONS.TITLE');
  }

  ionViewWillEnter() {
    this.titleService.setTitle('NOTIFICATIONS.TITLE');
  }

  async onSubmit() {
    //console.log('Updated Notification Settings:', this.notificationForm.value);
    const toast = await this.toastCtrl.create({
      message: 'Notification settings saved.',
      duration: 3000,
      color: 'success',
      position: 'bottom'
    });
    await toast.present();
  }
}
