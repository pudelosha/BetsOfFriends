import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToastController, LoadingController } from '@ionic/angular';
import { CustomTournamentService } from 'src/app/services/custom-tournament.service';
import { TournamentSelectionService } from 'src/app/services/tournament-selection.service';
import { TournamentSummary } from 'src/app/model/tournament-model';
import { ModalController } from '@ionic/angular';
import { PlayerStatsModalComponent } from 'src/app/modals/player-stats-modal/player-stats-modal.component';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { TitleService } from 'src/app/services/title.service';
import { IonContent, IonGrid, IonRow, IonCol, IonProgressBar, IonSpinner } from '@ionic/angular/standalone';

@Component({
  selector: 'app-tournament-results',
  templateUrl: './tournament-results.page.html',
  styleUrls: ['./tournament-results.page.scss'],
  standalone: true,
  imports: [CommonModule, TranslateModule, IonContent, IonGrid, IonRow, IonCol, IonProgressBar, IonSpinner],
})
export class TournamentResultsPage implements OnInit {
  tournamentId: number | null = null;
  summaryData: TournamentSummary[] = [];
  isLoading = true;

  constructor(
    private tournamentService: CustomTournamentService,
    private tournamentSelectionService: TournamentSelectionService,
    private toastController: ToastController,
    private loadingController: LoadingController,
    private modalController: ModalController,
    private titleService: TitleService,
    private translate: TranslateService
  ) {}

  async ngOnInit() {
    //await this.loadTournamentAndFetchSummary();
  }

  async ionViewWillEnter() {
    this.titleService.setTitle('RESULTS.TITLE');
    await this.loadTournamentAndFetchSummary();
  }

  private async loadTournamentAndFetchSummary() {
    this.tournamentId = this.tournamentSelectionService.getSelectedTournament();
    
    if (this.tournamentId === null) {
      await this.showToast(this.t('TOASTS.NO_TOURNAMENT_SELECTED'), 'warning');
      this.isLoading = false;
      return;
    }

    await this.fetchSummary();
  }

  async fetchSummary() {
    if (this.tournamentId === null) {
      console.error('Tournament ID is null, cannot fetch results.');
      return;
    }
  
    const loading = await this.loadingController.create({
      message: this.t('TOASTS.LOADING_RESULTS'),
      spinner: 'crescent',
    });
    await loading.present();
  
    const startTime = Date.now();
  
    this.tournamentService.getTournamentSummary(this.tournamentId).subscribe({
      next: async (summary) => {
        this.summaryData = summary;
        this.isLoading = false;
  
        const elapsedTime = Date.now() - startTime;
        const delay = Math.max(0, 500 - elapsedTime);
  
        setTimeout(async () => {
          await loading.dismiss();
        }, delay);
      },
      error: async (error) => {
        console.error('Error fetching results:', error);
        this.isLoading = false;
  
        const elapsedTime = Date.now() - startTime;
        const delay = Math.max(0, 500 - elapsedTime);
  
        setTimeout(async () => {
          await loading.dismiss();
          await this.showToast(this.t('TOASTS.RESULTS_LOAD_FAILED'), 'danger');
        }, delay);
      },
    });
  }
  
  async openPlayerStats(userId: string) {
    const tournamentId = this.tournamentSelectionService.getSelectedTournament();
    
    if (!tournamentId) {
      console.error("No tournament selected.");
      return;
    }
  
    const modal = await this.modalController.create({
      component: PlayerStatsModalComponent,
      componentProps: { tournamentId, userId },
      breakpoints: [1],
      initialBreakpoint: 1
    });
  
    await modal.present();
  }

  get showQualifiedColumn(): boolean {
    return this.summaryData?.length ? this.summaryData[0].showQualified : false;
  }
  
  get showExactResultColumn(): boolean {
    return this.summaryData?.length ? this.summaryData[0].showExactResult : false;
  }
  
  calculatePlayerColumnSize(): number {
    const baseSize = 4;
    let extra = 0;
    if (!this.showQualifiedColumn) extra += 1;
    if (!this.showExactResultColumn) extra += 1;
    return baseSize + extra;
  }

  getDisplayPosition(index: number): number {
    if (index <= 0) {
      return 1;
    }

    const current = this.summaryData[index];
    const previous = this.summaryData[index - 1];

    if (this.roundPoints(current?.totalPayout) === this.roundPoints(previous?.totalPayout)) {
      return this.getDisplayPosition(index - 1);
    }

    return index + 1;
  }

  private roundPoints(value?: number | null): number | null {
    return value === null || value === undefined ? null : Math.round(value * 100) / 100;
  }
          
  async showToast(message: string, color: 'success' | 'warning' | 'danger' | 'primary') {
    const toast = await this.toastController.create({
      message,
      duration: 3000,
      position: 'bottom',
      color,
    });
    await toast.present();
  }

  private t(key: string, params?: Record<string, unknown>): string {
    return this.translate.instant(key, params);
  }
}
