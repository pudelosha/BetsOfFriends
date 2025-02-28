import { Component, CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { IonicModule } from '@ionic/angular';
import { LatestMessagesPage } from '../latest-messages/latest-messages.page';
import { PendingBetsPage } from '../pending-bets/pending-bets.page';
import { TournamentSummaryPage } from '../tournament-summary/tournament-summary.page';


@Component({
  selector: 'app-home',
  templateUrl: './home.page.html',
  styleUrls: ['./home.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule, LatestMessagesPage, PendingBetsPage, TournamentSummaryPage],
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
})
export class HomePage {

  constructor() {}




}
