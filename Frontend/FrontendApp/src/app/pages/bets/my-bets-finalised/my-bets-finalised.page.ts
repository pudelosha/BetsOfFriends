import { Component, Input, OnInit, OnChanges, SimpleChanges, ChangeDetectorRef  } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToastController, ModalController, LoadingController } from '@ionic/angular';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { BetService } from 'src/app/services/bet.service';
import { TournamentSelectionService } from 'src/app/services/tournament-selection.service';
import { Bet, BetStats } from 'src/app/model/bet';
import { firstValueFrom } from 'rxjs';
import { BetsOverviewModalComponent } from 'src/app/modals/bets-overview-modal/bets-overview-modal.component';
import { HttpErrorResponse } from '@angular/common/http';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { IonSpinner, IonList, IonItem, IonButton, IonIcon } from '@ionic/angular/standalone';

@Component({
  selector: 'app-my-bets-finalised',
  templateUrl: './my-bets-finalised.page.html',
  styleUrls: ['./my-bets-finalised.page.scss'],
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, TranslateModule, IonSpinner, IonList, IonItem, IonButton, IonIcon],
})
export class MyBetsFinalisedPage implements OnInit, OnChanges {
  @Input() stage!: string;

  bets: Bet[] = [];
  isLoading = true;
  errorMessage: string = '';

  constructor(
    private modalCtrl: ModalController,
    private betService: BetService,
    private tournamentSelectionService: TournamentSelectionService,
    private toastController: ToastController,
    private loadingController: LoadingController,
    private translate: TranslateService
  ) {}

  ngOnInit() {
    this.loadBets();
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['stage'] && !changes['stage'].firstChange) {
      this.loadBets();
    }
  }

  ionViewWillEnter() {
    this.loadBets();
  }

  async loadBets() {
    if (!this.stage) {
      return;
    }

    this.isLoading = true;
    this.bets = [];
    this.errorMessage = '';
  
    const loading = await this.loadingController.create({
      message: this.t('TOASTS.LOADING_BETS'),
      spinner: 'crescent',
    });
    await loading.present();
  
    const startTime = Date.now();
  
    const tournamentId = this.tournamentSelectionService.getSelectedTournament();
  
    if (!tournamentId) {
      console.warn("No tournament selected.");
      this.errorMessage = this.t('TOASTS.NO_TOURNAMENT_SELECTED');
      this.isLoading = false;
      if (document.activeElement instanceof HTMLElement) {
        document.activeElement.blur();
      }
      await loading.dismiss();
      return;
    }
  
    try {
      this.bets = await firstValueFrom(
        this.betService.getBetsByTournamentStage(tournamentId, 'Closed', this.stage)
      );
  
      if (!this.bets.length) {
        this.errorMessage = this.t('TOASTS.NO_BETS_FOR_STAGE');
      }
    } catch (error: unknown) {
      console.error("API error:", error);
  
      if (error instanceof HttpErrorResponse) {
        this.errorMessage = this.t('TOASTS.ERROR_OCCURRED', { message: error.message });
      } else {
        this.errorMessage = this.t('TOASTS.UNEXPECTED_ERROR');
      }
    } finally {
      const elapsedTime = Date.now() - startTime;
      const delay = Math.max(0, 200 - elapsedTime);
  
      setTimeout(async () => {
        this.isLoading = false;
        if (document.activeElement instanceof HTMLElement) {
          document.activeElement.blur();
        }
        await loading.dismiss();
      }, delay);
    }
  }  
  
  getBetStatus(bet: Bet): string {
    const ph = bet.playerHomeGoals;
    const pa = bet.playerAwayGoals;
    const ah = bet.actualHomeGoals;
    const aa = bet.actualAwayGoals;
    const status = bet.matchStatus?.toLowerCase();
  
    // 1. Game is in progress (based on match status from backend)
    if (bet.status === 'Closed' && status === 'in_play') {
      return 'In Progress';
    }
  
    // 2. Prediction not made
    if (ph == null || pa == null) {
      return 'Not Predicted';
    }
  
    // 3. Final result not yet available
    if (ah == null || aa == null) {
      return 'Not Finalized';
    }
  
    // 4. Exact match predicted
    if (ph === ah && pa === aa) {
      return 'Exact Match';
    }
  
    // 5. Determine outcome
    const predicted = ph > pa ? 'home' : ph < pa ? 'away' : 'draw';
    const actual = ah > aa ? 'home' : ah < aa ? 'away' : 'draw';
  
    return predicted === actual ? 'Won' : 'Lost';
  }
      
  async openBetsOverview(bet: Bet) {  
    try {
      const betStats: BetStats = await firstValueFrom(this.betService.getBetStatsByMatchId(bet.matchId));  
      const modal = await this.modalCtrl.create({
        component: BetsOverviewModalComponent,
        componentProps: { betStats },
        breakpoints: [0, 0.75, 1],
        initialBreakpoint: 1,
      });
  
      await modal.present();
  
    } catch (error) {
      console.error("Error fetching bet overview data:", error);
      this.showToast(this.t('TOASTS.BET_OVERVIEW_FAILED'), "danger");
    }
  }   

  async showToast(message: string, color: 'success' | 'warning' | 'danger') {
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
