import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ModalController, ToastController } from '@ionic/angular';
import { IonHeader, IonToolbar, IonTitle, IonButtons, IonButton, IonContent, IonGrid, IonRow, IonCol, IonIcon } from '@ionic/angular/standalone';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';
import { BetStats } from 'src/app/model/bet';
import { BetService } from 'src/app/services/bet.service';
import { PendingBetRemindersModalComponent } from '../pending-bet-reminders-modal/pending-bet-reminders-modal.component';

@Component({
  selector: 'app-bets-overview-modal',
  templateUrl: './bets-overview-modal.component.html',
  styleUrls: ['./bets-overview-modal.component.scss'],
  standalone: true,
  imports: [CommonModule, TranslateModule, IonHeader, IonToolbar, IonTitle, IonButtons, IonButton, IonContent, IonGrid, IonRow, IonCol, IonIcon],
})
export class BetsOverviewModalComponent {
  @Input() betStats: BetStats = {
    matchId: 0,
    showExactResult: null,
    showQualified: null,
    matchStatus: null,
    homeTeam: '',
    awayTeam: '',
    homeScoreUser: null,
    awayScoreUser: null,
    homeScoreActual: null,
    awayScoreActual: null,
    qualifiedTeam: null,
    percent1: 0,
    percentX: 0,
    percent2: 0,
    percent1Q: null,
    percent2Q: null,
    placedBetsCount: 0,
    participantsCount: 0,
    canSendPendingBetReminders: false,
    pendingBetReminderCount: 0,
    averageHomeGoals: null,
    averageAwayGoals: null,
    result: null,
    resultQualified: null
  };

  loadingPendingReminders = false;

  constructor(
    private modalCtrl: ModalController,
    private toastController: ToastController,
    private translate: TranslateService,
    private betService: BetService
  ) {}

  closeModal() {
    this.modalCtrl.dismiss();
  }

  calculatePlayerColumnSize(): number {
    let used = 2 + 1 + 1 + 1;
  
    if (this.betStats?.showQualified) used += 2;
    if (this.betStats?.showExactResult) used += 1;
  
    return 12 - used;
  }  

  formatAverageGoals(value?: number | null): string {
    return value === null || value === undefined ? '-' : value.toFixed(1);
  }

  formatPercent(value?: number | null): string {
    return value === null || value === undefined ? '-' : value.toFixed(1);
  }

  shouldShowUserBets(): boolean {
    return this.betStats.matchStatus === 'In_Play' || this.betStats.matchStatus === 'Finished';
  }

  isMatchLive(): boolean {
    return this.betStats.matchStatus === 'In_Play';
  }

  canOpenPendingReminders(): boolean {
    return !!this.betStats.canSendPendingBetReminders &&
      (this.betStats.pendingBetReminderCount ?? 0) > 0 &&
      !this.shouldShowUserBets();
  }

  async openPendingBetReminders(): Promise<void> {
    if (!this.canOpenPendingReminders() || this.loadingPendingReminders) {
      return;
    }

    this.loadingPendingReminders = true;

    try {
      const summary = await firstValueFrom(this.betService.getPendingBetReminders(this.betStats.matchId));

      const modal = await this.modalCtrl.create({
        component: PendingBetRemindersModalComponent,
        componentProps: {
          matchId: summary.matchId,
          participants: summary.participants
        }
      });

      await modal.present();
    } catch {
      await this.showToast(this.t('BETS_OVERVIEW.PENDING_REMINDERS_LOAD_FAILED'), 'danger');
    } finally {
      this.loadingPendingReminders = false;
    }
  }

  private async showToast(message: string, color: 'danger'): Promise<void> {
    const toast = await this.toastController.create({
      message,
      duration: 3000,
      position: 'bottom',
      color
    });
    await toast.present();
  }

  private t(key: string, params?: Record<string, unknown>): string {
    return this.translate.instant(key, params);
  }
}
