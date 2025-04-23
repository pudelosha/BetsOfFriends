import { Component, CUSTOM_ELEMENTS_SCHEMA, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { LatestMessagesPage } from '../latest-messages/latest-messages.page';
import { UpcomingBetsPage } from '../upcoming-bets/upcoming-bets.page';
import { TournamentSummaryPage } from '../tournament-summary/tournament-summary.page';
import { TournamentInvitesPage } from '../tournament-invites/tournament-invites.page';
import { StartedCustomMatchesPage } from '../started-custom-matches/started-custom-matches.page';
import { StartedPredefinedMatchesPage } from '../started-predefined-matches/started-predefined-matches.page';
import { SelectedTournamentPage } from '../selected-tournament/selected-tournament.page';
import { LoadingController } from '@ionic/angular';
import { TitleService } from 'src/app/services/title.service';
import { IonContent } from '@ionic/angular/standalone';

@Component({
  selector: 'app-home',
  templateUrl: './home.page.html',
  styleUrls: ['./home.page.scss'],
  standalone: true,
  imports: [CommonModule, IonContent, ReactiveFormsModule, LatestMessagesPage, SelectedTournamentPage, UpcomingBetsPage, TournamentSummaryPage, TournamentInvitesPage, StartedPredefinedMatchesPage, StartedCustomMatchesPage],
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
})
export class HomePage implements OnInit {
  refreshCounter = 0;
  private loadingCount = 0;
  private loading: HTMLIonLoadingElement | null = null;

  constructor(private loadingController: LoadingController,
              private titleService: TitleService
  ) {}

  ngOnInit() {
    this.titleService.setTitle('HOME.TITLE');
    this.triggerRefresh();
  }

  ionViewWillEnter() {
    this.titleService.setTitle('HOME.TITLE');
    this.triggerRefresh();
  }

  triggerRefresh() {
    this.refreshCounter++;
  }

  async showGlobalSpinner() {
    this.loadingCount++;
    if (this.loadingCount === 1) {
      this.loading = await this.loadingController.create({
        message: 'Loading...',
        spinner: 'crescent',
      });
      await this.loading.present();
    }
  }

  async hideGlobalSpinner() {
    this.loadingCount = Math.max(0, this.loadingCount - 1);
    if (this.loadingCount === 0 && this.loading) {
      await this.loading.dismiss();
      this.loading = null;
    }
  }
}