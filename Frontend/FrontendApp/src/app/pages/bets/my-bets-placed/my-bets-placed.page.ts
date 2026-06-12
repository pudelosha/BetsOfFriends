import { ModalController, ToastController, LoadingController } from '@ionic/angular';
import { EditBetModalComponent } from 'src/app/modals/edit-bet-modal/edit-bet-modal.component';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { BetService } from 'src/app/services/bet.service';
import { TournamentSelectionService } from 'src/app/services/tournament-selection.service';
import { Bet, BetUpdateDto, BetStats } from 'src/app/model/bet';
import { firstValueFrom } from 'rxjs';
import { BetsOverviewModalComponent } from 'src/app/modals/bets-overview-modal/bets-overview-modal.component';
import { Component, Input, OnInit, OnChanges, SimpleChanges  } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { IonSpinner, IonList, IonItem, IonButton, IonIcon } from '@ionic/angular/standalone';

@Component({
  selector: 'app-my-bets-placed',
  templateUrl: './my-bets-placed.page.html',
  styleUrls: ['./my-bets-placed.page.scss'],
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, TranslateModule, IonSpinner, IonList, IonItem, IonButton, IonIcon],
})
export class MyBetsPlacedPage implements OnInit, OnChanges {
  @Input() stage!: string;
  
  bets: Bet[] = [];
  isLoading = true;
  errorMessage: string = '';
  private loadSequence = 0;

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
    const requestedStage = this.stage;

    if (!requestedStage) {
      return;
    }

    const sequence = ++this.loadSequence;
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
      if (sequence === this.loadSequence) {
        this.errorMessage = this.t('TOASTS.NO_TOURNAMENT_SELECTED');
        this.isLoading = false;
      }
      if (document.activeElement instanceof HTMLElement) {
        document.activeElement.blur();
      }
      await loading.dismiss();
      return;
    }
  
    try {
      const bets = await firstValueFrom(
        this.betService.getBetsByTournamentStage(tournamentId, 'Placed', requestedStage)
      );

      if (sequence !== this.loadSequence) {
        return;
      }

      this.bets = bets;
      this.errorMessage = '';
  
      if (!bets.length) {
        this.errorMessage = this.t('TOASTS.NO_BETS_FOR_STAGE');
      }
    } catch (error: unknown) {
      if (sequence !== this.loadSequence) {
        return;
      }

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
        if (sequence === this.loadSequence) {
          this.isLoading = false;
          if (document.activeElement instanceof HTMLElement) {
            document.activeElement.blur();
          }
        }
        await loading.dismiss();
      }, delay);
    }
  }
    
  async editBet(bet: Bet, event: Event) {
    event.stopPropagation();
  
    if (!bet) {
      console.error("Error: bet is undefined or null!");
      return;
    }

    if (!this.isBetOpenForEditing(bet)) {
      await this.loadBets();
      await this.showToast(this.t('TOASTS.BET_CLOSED'), "warning");
      return;
    }
  
    const modal = await this.modalCtrl.create({
      component: EditBetModalComponent,
      componentProps: { bet },
      breakpoints: [0, 0.5, 0.75, 1], 
      initialBreakpoint: 1, 
    });
  
    await modal.present();
  
    const { data } = await modal.onWillDismiss();
    if (data) {
  
      const betUpdate: BetUpdateDto = {
        baseAmount: 1,
        bonusAmount: null,
        homeGoals: data.playerHomeGoals,
        awayGoals: data.playerAwayGoals,
        qualifiedTeam: data.playerQualifiedTeam,
      };
  
      try {
        await firstValueFrom(this.betService.updateBet(bet.betId, betUpdate));
  
        await this.loadBets(); 
  
        this.showToast(this.t('TOASTS.BET_UPDATED'), "success");
      } catch (error) {
        console.error("Error updating bet:", error);

        if (!this.isBetOpenForEditing(bet)) {
          await this.loadBets();
          await this.showToast(this.t('TOASTS.BET_CLOSED'), "warning");
          return;
        }

        this.showToast(this.t('TOASTS.BET_UPDATE_FAILED'), "danger");
      }
    }
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

  private isBetOpenForEditing(bet: Bet): boolean {
    const matchStart = new Date(bet.startTime).getTime();
    const matchStatus = bet.matchStatus?.toLowerCase();

    return Number.isFinite(matchStart) &&
      matchStart > Date.now() &&
      (matchStatus === 'scheduled' || matchStatus === 'timed');
  }
}
