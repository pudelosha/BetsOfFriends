import { Component, CUSTOM_ELEMENTS_SCHEMA, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { LatestMessagesPage } from '../latest-messages/latest-messages.page';
import { UpcomingBetsPage } from '../upcoming-bets/upcoming-bets.page';
import { TournamentResultsPage } from '../tournament-results/tournament-results.page';
import { TournamentInvitesPage } from '../tournament-invites/tournament-invites.page';
import { StartedCustomMatchesPage } from '../started-custom-matches/started-custom-matches.page';
import { StartedPredefinedMatchesPage } from '../started-predefined-matches/started-predefined-matches.page';
import { SelectedTournamentPage } from '../selected-tournament/selected-tournament.page';
import { LoadingController } from '@ionic/angular';
import { TitleService } from 'src/app/services/title.service';
import { IonContent } from '@ionic/angular/standalone';
import { TournamentMessageBoardPage } from "../tournament-message-board/tournament-message-board.page";
import { TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-home',
  templateUrl: './home.page.html',
  styleUrls: ['./home.page.scss'],
  standalone: true,
  imports: [CommonModule, IonContent, ReactiveFormsModule, LatestMessagesPage, SelectedTournamentPage, UpcomingBetsPage, TournamentResultsPage, TournamentInvitesPage, StartedPredefinedMatchesPage, StartedCustomMatchesPage, TournamentMessageBoardPage],
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
})
export class HomePage implements OnDestroy {
  refreshCounter = 0;
  private loadingCount = 0;
  private loading: HTMLIonLoadingElement | null = null;
  private spinnerSync: Promise<void> = Promise.resolve();

  constructor(private loadingController: LoadingController,
              private titleService: TitleService,
              private translate: TranslateService
  ) {}

  ionViewDidEnter() {
    this.titleService.setTitle('HOME.TITLE');
    this.triggerRefresh();
  }

  ionViewWillLeave() {
    this.loadingCount = 0;
    this.queueSpinnerSync();
  }

  ngOnDestroy() {
    this.loadingCount = 0;
    this.queueSpinnerSync();
  }

  triggerRefresh() {
    this.refreshCounter++;
  }

  showGlobalSpinner() {
    this.loadingCount++;
    this.queueSpinnerSync();
  }

  hideGlobalSpinner() {
    this.loadingCount = Math.max(0, this.loadingCount - 1);
    this.queueSpinnerSync();
  }

  private queueSpinnerSync() {
    this.spinnerSync = this.spinnerSync
      .then(() => this.syncSpinnerState())
      .catch(error => {
        console.error('Failed to update home loading spinner:', error);
        this.loading = null;
      });
  }

  private async syncSpinnerState() {
    if (this.loadingCount > 0) {
      if (this.loading) {
        return;
      }

      const loading = await this.loadingController.create({
        message: this.translate.instant('TOASTS.LOADING'),
        spinner: 'crescent',
      });

      if (this.loadingCount === 0) {
        return;
      }

      this.loading = loading;
      await loading.present();

      if (this.loadingCount === 0) {
        await this.dismissGlobalSpinner();
      }
      return;
    }

    await this.dismissGlobalSpinner();
  }

  private async dismissGlobalSpinner() {
    const loading = this.loading;
    if (!loading) {
      return;
    }

    this.loading = null;
    await loading.dismiss();
  }
}
