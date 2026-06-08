import { Component, OnInit, Input, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TournamentParticipant } from 'src/app/model/tournament-model';
import { CustomTournamentService } from 'src/app/services/custom-tournament.service';
import { TournamentSelectionService } from 'src/app/services/tournament-selection.service';
import { firstValueFrom } from 'rxjs';
import { ToastController, LoadingController, AlertController } from '@ionic/angular';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { IonList, IonItem, IonButton, IonSpinner } from '@ionic/angular/standalone';
import { BackendMessageService } from 'src/app/services/backend-message.service';

@Component({
  selector: 'app-pending-invites',
  standalone: true,
  imports: [CommonModule, TranslateModule, IonList, IonItem, IonButton, IonSpinner],
  templateUrl: './pending-invites.page.html',
  styleUrls: ['./pending-invites.page.scss']
})
export class PendingInvitesPage implements OnInit {
  @Input() refreshTrigger: number = 0;

  tournamentId: number | null = null;
  pendingParticipants: TournamentParticipant[] = [];
  isLoading = true;

  constructor(
    private tournamentService: CustomTournamentService,
    private tournamentSelectionService: TournamentSelectionService,
    private toastController: ToastController,
    private loadingController: LoadingController,
    private alertController: AlertController,
    private translate: TranslateService,
    private backendMessages: BackendMessageService
  ) {}

  async ngOnInit() {
    await this.loadPendingInvites();
  }

  async ngOnChanges(changes: SimpleChanges) {
    if (changes['refreshTrigger'] && !changes['refreshTrigger'].firstChange) {
      this.loadPendingInvites();
    }
  }

  async loadPendingInvites() {
    this.isLoading = true;
    this.tournamentId = this.tournamentSelectionService.getSelectedTournament();

    if (this.tournamentId === null) {
      this.isLoading = false;
      return;
    }

    const loading = await this.loadingController.create({
      message: this.t('PENDING_INVITES.LOADING_INVITES'),
      spinner: 'crescent'
    });
    await loading.present();

    try {
      this.pendingParticipants = await firstValueFrom(
        this.tournamentService.getTournamentParticipants(this.tournamentId, 'Invited')
      );
    } catch (error) {
      console.error('Error loading invited participants:', error);
      await this.showToast(this.t('PENDING_INVITES.LOAD_FAILED'), 'danger');
    } finally {
      this.isLoading = false;
      loading.dismiss();
    }
  }

  async confirmResendInvite(email: string) {
    const alert = await this.alertController.create({
      header: this.t('PENDING_INVITES.RESEND_TITLE'),
      message: this.t('PENDING_INVITES.RESEND_CONFIRMATION', { email }),
      buttons: [
        {
          text: this.t('PENDING_INVITES.CANCEL'),
          role: 'cancel'
        },
        {
          text: this.t('PENDING_INVITES.RESEND'),
          handler: () => {
            this.resendInvite(email);
          }
        }
      ]
    });
    await alert.present();
  }
  
  async confirmExcludeInvite(email: string) {
    const alert = await this.alertController.create({
      header: this.t('USERS_LIST.EXCLUDE_TITLE'),
      message: this.t('USERS_LIST.EXCLUDE_CONFIRMATION', { email }),
      buttons: [
        {
          text: this.t('PENDING_INVITES.CANCEL'),
          role: 'cancel'
        },
        {
          text: this.t('PENDING_INVITES.EXCLUDE'),
          role: 'destructive',
          handler: () => {
            this.excludeInvite(email);
          }
        }
      ]
    });
    await alert.present();
  }
  
  resendInvite(email: string) {
    this.tournamentService.resendParticipantInvite(this.tournamentId!, email).subscribe({
      next: async (result) => {
        if (result.success) {
          await this.showToast(this.t('PENDING_INVITES.RESENT', { email }), 'success');
        } else {
          await this.showToast(
            this.backendMessages.translateMessage(result.message, 'PENDING_INVITES.RESEND_FAILED'),
            'danger'
          );
        }
      },
      error: async (error) => {
        console.error('Error resending invite:', error);
        await this.showToast(
          this.backendMessages.translateMessage(this.extractBackendMessage(error), 'PENDING_INVITES.RESEND_FAILED'),
          'danger'
        );
      }
    });
  }
    
  excludeInvite(userEmail: string) {  
    this.tournamentId = this.tournamentSelectionService.getSelectedTournament();
  
    this.tournamentService.excludeParticipant(this.tournamentId!, userEmail).subscribe({
      next: async (result) => {
        if (result.success) {
          await this.showToast(this.t('USERS_LIST.EXCLUDED', { email: userEmail }), 'success');
          await this.loadPendingInvites();
        } else {
          await this.showToast(
            this.backendMessages.translateMessage(result.message, 'USERS_LIST.EXCLUDE_FAILED'),
            'danger'
          );
        }
      },
      error: async (err) => {
        console.error('Error excluding participant:', err);
        await this.showToast(
          this.backendMessages.translateMessage(this.extractBackendMessage(err), 'USERS_LIST.EXCLUDE_FAILED'),
          'danger'
        );
      }
    });
  }
    
  async showToast(message: string, color: 'success' | 'warning' | 'danger') {
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

  private extractBackendMessage(error: unknown): string | undefined {
    const maybeHttpError = error as { error?: { message?: string; Message?: string } };
    return maybeHttpError?.error?.message || maybeHttpError?.error?.Message;
  }
}
