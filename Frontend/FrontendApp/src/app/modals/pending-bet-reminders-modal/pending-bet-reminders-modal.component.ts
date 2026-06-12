import { CommonModule } from '@angular/common';
import { Component, Input, OnInit } from '@angular/core';
import { ModalController, ToastController } from '@ionic/angular';
import { IonButton, IonButtons, IonContent, IonHeader, IonIcon, IonItem, IonLabel, IonList, IonTitle, IonToolbar } from '@ionic/angular/standalone';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';
import { PendingBetReminderParticipant } from 'src/app/model/bet';
import { BetService } from 'src/app/services/bet.service';

@Component({
  selector: 'app-pending-bet-reminders-modal',
  templateUrl: './pending-bet-reminders-modal.component.html',
  styleUrls: ['./pending-bet-reminders-modal.component.scss'],
  standalone: true,
  imports: [CommonModule, TranslateModule, IonHeader, IonToolbar, IonTitle, IonButtons, IonButton, IonContent, IonList, IonItem, IonLabel, IonIcon],
})
export class PendingBetRemindersModalComponent implements OnInit {
  @Input() matchId!: number;
  @Input() participants: PendingBetReminderParticipant[] = [];

  sendingAll = false;
  sendingUserIds = new Set<string>();
  private sentUserIds = new Set<string>();
  private newlyRemindedUserIds = new Set<string>();

  constructor(
    private modalCtrl: ModalController,
    private toastController: ToastController,
    private translate: TranslateService,
    private betService: BetService
  ) {}

  ngOnInit(): void {
    this.sentUserIds = new Set(
      this.participants
        .filter(participant => participant.reminderSent)
        .map(participant => participant.userId)
    );
  }

  closeModal(): void {
    this.modalCtrl.dismiss({
      remindedUserIds: Array.from(this.newlyRemindedUserIds)
    });
  }

  hasUnsentParticipants(): boolean {
    return this.participants.some(participant => !this.isReminderSent(participant));
  }

  isReminderSent(participant: PendingBetReminderParticipant): boolean {
    return participant.reminderSent || this.sentUserIds.has(participant.userId);
  }

  isSending(userId: string): boolean {
    return this.sendingUserIds.has(userId);
  }

  async sendAll(): Promise<void> {
    const targetUserIds = this.participants
      .filter(participant => !this.isReminderSent(participant))
      .map(participant => participant.userId);

    if (!targetUserIds.length || this.sendingAll) {
      return;
    }

    this.sendingAll = true;
    targetUserIds.forEach(userId => this.sendingUserIds.add(userId));

    try {
      const result = await firstValueFrom(this.betService.sendPendingBetReminders(this.matchId, targetUserIds));
      this.markReminded(result.remindedUserIds);
      await this.showToast(
        this.t('BETS_OVERVIEW.PENDING_REMINDERS_SENT_TOAST', { count: result.remindedUserIds.length }),
        'success'
      );
    } catch {
      await this.showToast(this.t('BETS_OVERVIEW.PENDING_REMINDERS_FAILED_TOAST'), 'danger');
    } finally {
      targetUserIds.forEach(userId => this.sendingUserIds.delete(userId));
      this.sendingAll = false;
    }
  }

  async sendOne(participant: PendingBetReminderParticipant): Promise<void> {
    if (this.isReminderSent(participant) || this.isSending(participant.userId)) {
      return;
    }

    this.sendingUserIds.add(participant.userId);

    try {
      const result = await firstValueFrom(this.betService.sendPendingBetReminders(this.matchId, [participant.userId]));

      if (!result.remindedUserIds.includes(participant.userId)) {
        await this.showToast(this.t('BETS_OVERVIEW.PENDING_REMINDERS_FAILED_TOAST'), 'danger');
        return;
      }

      this.markReminded(result.remindedUserIds);
      await this.showToast(
        this.t('BETS_OVERVIEW.PENDING_REMINDERS_SENT_TOAST', { count: result.remindedUserIds.length }),
        'success'
      );
    } catch {
      await this.showToast(this.t('BETS_OVERVIEW.PENDING_REMINDERS_FAILED_TOAST'), 'danger');
    } finally {
      this.sendingUserIds.delete(participant.userId);
    }
  }

  private markReminded(userIds: string[]): void {
    userIds.forEach(userId => {
      this.sentUserIds.add(userId);
      this.newlyRemindedUserIds.add(userId);

      const participant = this.participants.find(item => item.userId === userId);
      if (participant) {
        participant.reminderSent = true;
      }
    });
  }

  private async showToast(message: string, color: 'success' | 'danger'): Promise<void> {
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
