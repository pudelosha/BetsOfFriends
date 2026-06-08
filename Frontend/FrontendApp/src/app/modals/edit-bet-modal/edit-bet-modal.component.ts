import { Component, Input, AfterViewInit, CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ModalController, ToastController } from '@ionic/angular';
import { IonHeader, IonToolbar, IonTitle, IonButtons, IonButton, IonContent, IonGrid, IonRow, IonCol, IonLabel, IonPicker, IonPickerColumn, IonPickerColumnOption, IonItem, IonSegment, IonSegmentButton } from '@ionic/angular/standalone';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { Bet } from '../../model/bet';

@Component({
  selector: 'app-edit-bet-modal',
  templateUrl: './edit-bet-modal.component.html',
  styleUrls: ['./edit-bet-modal.component.scss'],
  standalone: true,
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
  imports: [CommonModule, FormsModule, TranslateModule, IonHeader, IonToolbar, IonTitle, IonButtons, IonButton, IonContent, IonGrid, IonRow, IonCol, IonLabel, IonPicker, IonPickerColumn, IonPickerColumnOption, IonItem, IonSegment, IonSegmentButton],
})
export class EditBetModalComponent implements AfterViewInit {
  @Input() set bet(value: Bet) {
    if (!value) {
      console.error("Received undefined or null bet!");
      return;
    }
    this._bet = value;
    this.homeGoals = value.playerHomeGoals ?? 0;
    this.awayGoals = value.playerAwayGoals ?? 0;
    this.actualQualifiedTeam = value.actualQualifiedTeam ?? null;
    this.playerQualifiedTeam = value.playerQualifiedTeam ?? 'Neutral';
  }
  get bet(): Bet {
    return this._bet;
  }

  private _bet!: Bet; 
  goalOptions = Array.from({ length: 11 }, (_, i) => i); // 0 to 10
  homeGoals: number = 0;
  awayGoals: number = 0;
  actualQualifiedTeam: 'Home' | 'Away' | null = null;
  playerQualifiedTeam: 'Home' | 'Away' | 'Neutral' | null = 'Neutral'

  constructor(
    private modalCtrl: ModalController,
    private toastController: ToastController,
    private translate: TranslateService
  ) {}

  ngAfterViewInit() {}

  onHomeGoalsChange(event: CustomEvent) {
    this.homeGoals = event.detail.value;
  }
  
  onAwayGoalsChange(event: CustomEvent) {
    this.awayGoals = event.detail.value;
  }

  saveBet() {
    if (!this._bet) {
      console.error("Error: Bet is undefined!");
      return;
    }
  
    const qualificationRequired = this._bet.showWhoQualifies &&
                                  this._bet.type === 'ExtendedWithQualification' &&
                                  this._bet.qualifyHomeOdds !== null && 
                                  this._bet.qualifyAwayOdds !== null;
  
    if (qualificationRequired && this.playerQualifiedTeam === 'Neutral') {
      this.showToast(this.t('TOASTS.SELECT_QUALIFYING_TEAM'), 'warning');
      return;
    }
  
    const emittedBet = {
      playerHomeGoals: this.homeGoals,
      playerAwayGoals: this.awayGoals,
      playerQualifiedTeam: this.playerQualifiedTeam,
      actualQualifiedTeam: this.actualQualifiedTeam
    };
    
    this.modalCtrl.dismiss(emittedBet);
  }
    
  closeModal() {
    this.modalCtrl.dismiss();
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
