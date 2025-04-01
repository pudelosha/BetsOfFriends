import { Component, OnInit, Input, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TournamentParticipant } from 'src/app/model/tournament-model';
import { CustomTournamentService } from 'src/app/services/custom-tournament.service';
import { TournamentSelectionService } from 'src/app/services/tournament-selection.service';
import { firstValueFrom } from 'rxjs';
import { IonicModule, ToastController, LoadingController, AlertController } from '@ionic/angular';
import { TranslateModule } from '@ngx-translate/core';


@Component({
  selector: 'app-pending-requests',
  standalone: true,
  imports: [CommonModule, IonicModule, TranslateModule],
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
    private alertController: AlertController
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
      await this.showToast('No tournament selected.', 'warning');
      this.isLoading = false;
      return;
    }

    const loading = await this.loadingController.create({
      message: 'Loading requests...',
      spinner: 'crescent'
    });
    await loading.present();

    try {
      this.pendingRequests = await firstValueFrom(
        this.tournamentService.getTournamentParticipants(this.tournamentId, 'Requested')
      );
    } catch (error) {
      console.error('Error loading requests:', error);
      await this.showToast('Failed to load requests.', 'danger');
    } finally {
      this.isLoading = false;
      loading.dismiss();
    }
  }

  async confirmAccept(email: string) {
    const alert = await this.alertController.create({
      header: 'Accept Request',
      message: `Do you want to accept the request from ${email}?`,
      buttons: [
        { text: 'Cancel', role: 'cancel' },
        {
          text: 'Accept',
          handler: () => this.acceptRequest(email)
        }
      ]
    });
    await alert.present();
  }

  async confirmReject(email: string) {
    const alert = await this.alertController.create({
      header: 'Reject Request',
      message: `Are you sure you want to reject the request from ${email}?`,
      buttons: [
        { text: 'Cancel', role: 'cancel' },
        {
          text: 'Reject',
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
          await this.showToast(result.message, 'success');
          await this.loadRequests();
        } else {
          await this.showToast(result.message, 'danger');
        }
      },
      error: async (error) => {
        console.error('Error accepting request:', error);
        await this.showToast('Failed to accept request.', 'danger');
      }
    });
  }
    
  rejectRequest(userEmail: string) {
    console.log('Confirming exclusion for:', userEmail);
  
    this.tournamentId = this.tournamentSelectionService.getSelectedTournament();
  
    this.tournamentService.excludeParticipant(this.tournamentId!, userEmail).subscribe({
      next: async (result) => {
        if (result.success) {
          await this.showToast(result.message, 'success');
          await this.loadRequests(); // Refresh the list
        } else {
          await this.showToast(result.message, 'danger');
        }
      },
      error: async (err) => {
        console.error('Error excluding participant:', err);
        await this.showToast('Failed to exclude participant.', 'danger');
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
}
