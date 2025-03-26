import { Component, CUSTOM_ELEMENTS_SCHEMA, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { IonicModule } from '@ionic/angular';
import { LatestMessagesPage } from '../latest-messages/latest-messages.page';
import { UpcomingBetsPage } from '../upcoming-bets/upcoming-bets.page';
import { TournamentSummaryPage } from '../tournament-summary/tournament-summary.page';
import { TournamentInvitesPage } from '../tournament-invites/tournament-invites.page';
import { StartedCustomMatchesPage } from '../started-custom-matches/started-custom-matches.page';

@Component({
  selector: 'app-home',
  templateUrl: './home.page.html',
  styleUrls: ['./home.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule, LatestMessagesPage, UpcomingBetsPage, TournamentSummaryPage, TournamentInvitesPage, StartedCustomMatchesPage],
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
})
export class HomePage implements OnInit {
  refreshCounter = 0;

  triggerRefresh() {
    this.refreshCounter++;
  }

  ngOnInit() {
  }

  ionViewWillEnter(){
    this.triggerRefresh();
  }

  constructor() { }

}
