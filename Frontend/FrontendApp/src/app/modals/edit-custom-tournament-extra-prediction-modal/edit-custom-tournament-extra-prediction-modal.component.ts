import { CommonModule } from '@angular/common';
import { Component, Input, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ModalController, ToastController } from '@ionic/angular';
import { IonButton, IonButtons, IonContent, IonHeader, IonInput, IonItem, IonLabel, IonSelect, IonSelectOption, IonTitle, IonToolbar } from '@ionic/angular/standalone';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { CustomTournamentExtraPrediction, CustomTournamentExtraPredictionFormValue, CustomTournamentExtraPredictionTeamOption } from 'src/app/model/custom-tournament-extra-prediction';

@Component({
  selector: 'app-edit-custom-tournament-extra-prediction-modal',
  templateUrl: './edit-custom-tournament-extra-prediction-modal.component.html',
  styleUrls: ['./edit-custom-tournament-extra-prediction-modal.component.scss'],
  standalone: true,
  imports: [CommonModule, FormsModule, TranslateModule, IonHeader, IonToolbar, IonTitle, IonButtons, IonButton, IonContent, IonItem, IonLabel, IonSelect, IonSelectOption, IonInput],
})
export class EditCustomTournamentExtraPredictionModalComponent implements OnInit {
  @Input() teams: CustomTournamentExtraPredictionTeamOption[] = [];
  @Input() prediction: CustomTournamentExtraPrediction | null = null;

  draft: CustomTournamentExtraPredictionFormValue = {
    winnerTeamId: null,
    secondPlaceTeamId: null,
    thirdPlaceTeamId: null,
    topScorerTeamId: null,
    topScorerName: ''
  };

  constructor(
    private modalCtrl: ModalController,
    private toastController: ToastController,
    private translate: TranslateService
  ) {}

  ngOnInit() {
    if (!this.prediction) {
      return;
    }

    this.draft = {
      winnerTeamId: this.prediction.winnerTeamId,
      secondPlaceTeamId: this.prediction.secondPlaceTeamId,
      thirdPlaceTeamId: this.prediction.thirdPlaceTeamId,
      topScorerTeamId: this.prediction.topScorerTeamId,
      topScorerName: this.prediction.topScorerName ?? ''
    };
  }

  isPodiumTeamDisabled(teamId: number, field: keyof Pick<CustomTournamentExtraPredictionFormValue, 'winnerTeamId' | 'secondPlaceTeamId' | 'thirdPlaceTeamId'>): boolean {
    return this.draft.winnerTeamId === teamId && field !== 'winnerTeamId' ||
      this.draft.secondPlaceTeamId === teamId && field !== 'secondPlaceTeamId' ||
      this.draft.thirdPlaceTeamId === teamId && field !== 'thirdPlaceTeamId';
  }

  async save() {
    if (this.hasPodiumDuplicates()) {
      await this.showToast(this.t('CUSTOM_TOURNAMENT_EXTRA_PREDICTIONS.DUPLICATE_TEAM'), 'warning');
      return;
    }

    this.modalCtrl.dismiss({
      ...this.draft,
      topScorerName: this.draft.topScorerName.trim()
    });
  }

  clear() {
    this.draft = {
      winnerTeamId: null,
      secondPlaceTeamId: null,
      thirdPlaceTeamId: null,
      topScorerTeamId: null,
      topScorerName: ''
    };
  }

  closeModal() {
    this.modalCtrl.dismiss();
  }

  private hasPodiumDuplicates(): boolean {
    const selectedTeamIds = [
      this.draft.winnerTeamId,
      this.draft.secondPlaceTeamId,
      this.draft.thirdPlaceTeamId
    ].filter((teamId): teamId is number => teamId !== null);

    return new Set(selectedTeamIds).size !== selectedTeamIds.length;
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
