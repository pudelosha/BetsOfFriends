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
  selector: 'app-pending-requests',
  standalone: true,
  imports: [CommonModule, TranslateModule, IonList, IonItem, IonButton, IonSpinner],
  templateUrl: './pending-requests.page.html',
  styleUrls: ['./pending-requests.page.scss']
})
export class PendingRequestsPage implements OnInit {
  @Input() refreshTrigger: number = 0;

  tournamentId: number | null = null;
  pendingRequests: TournamentParticipant[] = [];
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
    await this.loadRequests();
  }

  async ngOnChanges(changes: SimpleChanges) {
    if (changes['refreshTrigger'] && !changes['refreshTrigger'].firstChange) {
      this.loadRequests();
    }
  }

  async loadRequests() {
    this.isLoading = true;
    this.tournamentId = this.tournamentSelectionService.getSelectedTournament();

    if (!this.tournamentId) {
      this.isLoading = false;
      return;
    }

    const loading = await this.loadingController.create({
      message: this.t('PENDING_REQUESTS.LOADING_REQUESTS'),
      spinner: 'crescent'
    });
    await loading.present();

    try {
      this.pendingRequests = await firstValueFrom(
        this.tournamentService.getTournamentParticipants(this.tournamentId, 'Requested')
      );
    } catch (error) {
      console.error('Error loading requests:', error);
      await this.showToast(this.t('PENDING_REQUESTS.LOAD_FAILED'), 'danger');
    } finally {
      this.isLoading = false;
      loading.dismiss();
    }
  }

  async confirmAccept(email: string) {
    const alert = await this.alertController.create({
      header: this.t('PENDING_REQUESTS.ACCEPT_TITLE'),
      message: this.t('PENDING_REQUESTS.ACCEPT_CONFIRMATION', { email }),
      buttons: [
        { text: this.t('PENDING_REQUESTS.CANCEL'), role: 'cancel' },
        {
          text: this.t('PENDING_REQUESTS.ACCEPT'),
          handler: () => this.acceptRequest(email)
        }
      ]
    });
    await alert.present();
  }

  async confirmReject(email: string) {
    const alert = await this.alertController.create({
      header: this.t('PENDING_REQUESTS.REJECT_TITLE'),
      message: this.t('PENDING_REQUESTS.REJECT_CONFIRMATION', { email }),
      buttons: [
        { text: this.t('PENDING_REQUESTS.CANCEL'), role: 'cancel' },
        {
          text: this.t('PENDING_REQUESTS.REJECT'),
          role: 'destructive',
          handler: () => this.rejectRequest(email)
        }
      ]
    });
    await alert.present();
  }

  acceptRequest(email: string) {
    this.tournamentService.acceptParticipant(this.tournamentId!, email).subscribe({
      next: async (result) => {
        if (result.success) {
          await this.showToast(this.t('PENDING_REQUESTS.ACCEPTED', { email }), 'success');
          await this.loadRequests();
        } else {
          await this.showToast(
            this.backendMessages.translateMessage(result.message, 'PENDING_REQUESTS.ACCEPT_FAILED'),
            'danger'
          );
        }
      },
      error: async (error) => {
        console.error('Error accepting request:', error);
        await this.showToast(
          this.backendMessages.translateMessage(this.extractBackendMessage(error), 'PENDING_REQUESTS.ACCEPT_FAILED'),
          'danger'
        );
      }
    });
  }
    
  rejectRequest(userEmail: string) {
    this.tournamentId = this.tournamentSelectionService.getSelectedTournament();
  
    this.tournamentService.excludeParticipant(this.tournamentId!, userEmail).subscribe({
      next: async (result) => {
        if (result.success) {
          await this.showToast(this.t('PENDING_REQUESTS.REJECTED', { email: userEmail }), 'success');
          await this.loadRequests();
        } else {
          await this.showToast(
            this.backendMessages.translateMessage(result.message, 'PENDING_REQUESTS.REJECT_FAILED'),
            'danger'
          );
        }
      },
      error: async (err) => {
        console.error('Error excluding participant:', err);
        await this.showToast(
          this.backendMessages.translateMessage(this.extractBackendMessage(err), 'PENDING_REQUESTS.REJECT_FAILED'),
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
