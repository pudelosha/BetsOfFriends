import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonicModule, ToastController } from '@ionic/angular';
import { ReactiveFormsModule } from '@angular/forms';
import { FormBuilder, FormGroup } from '@angular/forms';


@Component({
  selector: 'app-notification-settings',
  templateUrl: './notification-settings.page.html',
  styleUrls: ['./notification-settings.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule],
})
export class NotificationSettingsPage implements OnInit {
  notificationForm: FormGroup;

  constructor(private fb: FormBuilder) {
    this.notificationForm = this.fb.group({
      tournamentResultsEmail: [false],
      tournamentResultsPush: [false],
      dailyTournamentUpdatesEmail: [false],
      dailyTournamentUpdatesPush: [false],
      tournamentInvitationsEmail: [false],
      tournamentInvitationsPush: [false],
      pendingBets1HourEmail: [false],
      pendingBets1HourPush: [false],
      pendingBets24HoursEmail: [false],
      pendingBets24HoursPush: [false],
      newGamesUpdatesEmail: [false],
      newGamesUpdatesPush: [false],
      liveMatchStartEmail: [false],
      liveMatchStartPush: [false],
      liveMatchSummaryEmail: [false],
      liveMatchSummaryPush: [false],
      specialOffersEmail: [false],
      specialOffersPush: [false]
    });
  }

  ngOnInit() {}

  onSubmit() {
    console.log('Notification Settings:', this.notificationForm.value);
  }
}