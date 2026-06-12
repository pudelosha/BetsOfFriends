import { Component, Input, OnInit, OnChanges, SimpleChanges  } from '@angular/core';
import { ModalController, ToastController, LoadingController } from '@ionic/angular';
import { EditMatchResultModalComponent } from 'src/app/modals/edit-match-result-modal/edit-match-result-modal.component';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { TournamentSelectionService } from 'src/app/services/tournament-selection.service';
import { firstValueFrom } from 'rxjs';
import { CustomMatchService } from 'src/app/services/custom-match.service';
import { Match } from 'src/app/model/match';
import { HttpErrorResponse } from '@angular/common/http';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { IonSpinner, IonList, IonItem, IonButton } from '@ionic/angular/standalone';

@Component({
  selector: 'app-manage-custom-upcoming',
  templateUrl: './manage-custom-upcoming.page.html',
  styleUrls: ['./manage-custom-upcoming.page.scss'],
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, TranslateModule, IonSpinner, IonList, IonItem, IonButton],
})
export class ManageCustomUpcomingPage implements OnInit, OnChanges  {
  @Input() stage!: string;

  matches: Match[] = [];
  isLoading = true;
  errorMessage: string = '';
  private loadSequence = 0;

  constructor(
    private modalCtrl: ModalController,
    private matchService: CustomMatchService,
    private tournamentSelectionService: TournamentSelectionService,
    private toastController: ToastController,
    private loadingController: LoadingController,
    private translate: TranslateService
  ) {}

  ngOnInit() {
    this.loadMatches(); 
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['stage'] && !changes['stage'].firstChange) {
      this.loadMatches();
    }
  }

  ionViewWillEnter() {
    this.loadMatches(); 
  }

  async loadMatches() {
    const requestedStage = this.stage;
    const sequence = ++this.loadSequence;

    this.isLoading = true;
    this.matches = [];
    this.errorMessage = '';

    if (!requestedStage) {
      this.isLoading = false;
      return;
    }
  
    const loading = await this.loadingController.create({
      message: this.t('TOASTS.LOADING_MATCHES'),
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
      await loading.dismiss();
      return;
    }
  
    try {
      const matches = await firstValueFrom(
        this.matchService.getMatchesByTournamentStage(tournamentId, 'Timed', requestedStage)
      );

      if (sequence !== this.loadSequence) {
        return;
      }

      this.matches = matches;
  
      if (!this.matches.length) {
        this.errorMessage = this.t('TOASTS.NO_MATCHES_FOR_STAGE');
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
        }
        await loading.dismiss();
      }, delay);
    }
  }
    
  async editMatchResult(match: Match, event: Event) {
    event.stopPropagation();
  
    const modal = await this.modalCtrl.create({
      component: EditMatchResultModalComponent,
      componentProps: { match },
      breakpoints: [0, 0.3, 0.5, 0.75, 1],
      initialBreakpoint: 1,
    });
  
    await modal.present();
  
    const { data } = await modal.onWillDismiss();
    if (data) {
  
      try {
        await firstValueFrom(this.matchService.updateMatchResult(match.matchId, data));
  
        await this.loadMatches();  
  
        this.showToast(this.t('TOASTS.MATCH_RESULT_UPDATED'), "success");
      } catch (error) {
        console.error("Error updating match result:", error);
        this.showToast(this.t('TOASTS.MATCH_RESULT_UPDATE_FAILED'), "danger");
      }
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
