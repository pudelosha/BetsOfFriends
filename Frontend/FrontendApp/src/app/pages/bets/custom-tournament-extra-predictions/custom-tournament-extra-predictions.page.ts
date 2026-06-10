import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ModalController, ToastController } from '@ionic/angular';
import { IonBadge, IonButton, IonContent, IonIcon, IonSpinner } from '@ionic/angular/standalone';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';
import { EditCustomTournamentExtraPredictionModalComponent } from 'src/app/modals/edit-custom-tournament-extra-prediction-modal/edit-custom-tournament-extra-prediction-modal.component';
import { CustomTournamentExtraPrediction, CustomTournamentExtraPredictionFormValue, CustomTournamentExtraPredictionTeamOption } from 'src/app/model/custom-tournament-extra-prediction';
import { CustomTournamentService } from 'src/app/services/custom-tournament.service';
import { TitleService } from 'src/app/services/title.service';
import { TournamentSelectionService } from 'src/app/services/tournament-selection.service';

interface ExtraPredictionRow {
  userName: string;
  isCurrentUser: boolean;
  winner: string;
  secondPlace: string;
  thirdPlace: string;
  topScorerTeam: string;
  topScorerName: string;
  hasPrediction: boolean;
}

@Component({
  selector: 'app-custom-tournament-extra-predictions',
  templateUrl: './custom-tournament-extra-predictions.page.html',
  styleUrls: ['./custom-tournament-extra-predictions.page.scss'],
  standalone: true,
  imports: [CommonModule, FormsModule, TranslateModule, IonContent, IonSpinner, IonButton, IonIcon, IonBadge],
})
export class CustomTournamentExtraPredictionsPage {
  isLoading = true;
  errorMessage = '';
  tournamentId: number | null = null;
  teams: CustomTournamentExtraPredictionTeamOption[] = [];
  rows: ExtraPredictionRow[] = [];
  myPrediction: CustomTournamentExtraPrediction | null = null;
  isLocked = false;

  constructor(
    private tournamentService: CustomTournamentService,
    private tournamentSelectionService: TournamentSelectionService,
    private modalCtrl: ModalController,
    private toastController: ToastController,
    private translate: TranslateService,
    private titleService: TitleService
  ) {}

  async ionViewWillEnter() {
    this.titleService.setTitle('CUSTOM_TOURNAMENT_EXTRA_PREDICTIONS.PAGE_TITLE');
    await this.loadData();
  }

  async openPredictionModal() {
    if (this.isLocked) {
      await this.showToast(this.t('CUSTOM_TOURNAMENT_EXTRA_PREDICTIONS.LOCKED_TOAST'), 'warning');
      return;
    }

    const modal = await this.modalCtrl.create({
      component: EditCustomTournamentExtraPredictionModalComponent,
      componentProps: {
        teams: this.teams,
        prediction: this.myPrediction
      },
      breakpoints: [0, 0.6, 0.85, 1],
      initialBreakpoint: 0.85,
    });

    await modal.present();

    const { data } = await modal.onWillDismiss<CustomTournamentExtraPredictionFormValue>();
    if (!data || this.tournamentId === null) {
      return;
    }

    try {
      await firstValueFrom(this.tournamentService.saveCustomTournamentExtraPrediction(this.tournamentId, data));
      await this.loadData();
      await this.showToast(this.t('CUSTOM_TOURNAMENT_EXTRA_PREDICTIONS.SAVED_LOCAL'), 'success');
    } catch (error) {
      console.error('Error saving custom tournament extra prediction:', error);
      const status = (error as { status?: number }).status;
      const messageKey = status === 409
        ? 'CUSTOM_TOURNAMENT_EXTRA_PREDICTIONS.LOCKED_TOAST'
        : 'CUSTOM_TOURNAMENT_EXTRA_PREDICTIONS.SAVE_FAILED';
      await this.showToast(this.t(messageKey), status === 409 ? 'warning' : 'danger');
    }
  }

  private async loadData() {
    this.isLoading = true;
    this.errorMessage = '';
    this.teams = [];
    this.rows = [];
    this.myPrediction = null;
    this.isLocked = false;

    this.tournamentId = this.tournamentSelectionService.getSelectedTournament();

    if (this.tournamentId === null) {
      this.isLoading = false;
      this.errorMessage = this.t('TOASTS.NO_TOURNAMENT_SELECTED');
      return;
    }

    try {
      const overview = await firstValueFrom(this.tournamentService.getCustomTournamentExtraPredictions(this.tournamentId));

      this.teams = overview.teams;
      this.isLocked = overview.isLocked;
      this.myPrediction = overview.predictions.find(prediction => prediction.isCurrentUser && prediction.hasPrediction) ?? null;
      this.rows = overview.predictions.map(prediction => this.createRow(prediction));
    } catch (error) {
      console.error('Error loading custom tournament extra predictions:', error);
      this.errorMessage = this.t('CUSTOM_TOURNAMENT_EXTRA_PREDICTIONS.LOAD_FAILED');
    } finally {
      this.isLoading = false;
    }
  }

  private createRow(prediction: CustomTournamentExtraPrediction): ExtraPredictionRow {
    return {
      userName: prediction.userName,
      isCurrentUser: prediction.isCurrentUser,
      winner: this.getTeamName(prediction.winnerTeamId),
      secondPlace: this.getTeamName(prediction.secondPlaceTeamId),
      thirdPlace: this.getTeamName(prediction.thirdPlaceTeamId),
      topScorerTeam: this.getTeamName(prediction.topScorerTeamId),
      topScorerName: prediction.topScorerName || '-',
      hasPrediction: prediction.hasPrediction
    };
  }

  private getTeamName(teamId: number | null): string {
    if (teamId === null) {
      return '-';
    }

    return this.teams.find(team => team.teamId === teamId)?.teamName ?? '-';
  }

  private async showToast(message: string, color: 'success' | 'warning' | 'danger') {
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
