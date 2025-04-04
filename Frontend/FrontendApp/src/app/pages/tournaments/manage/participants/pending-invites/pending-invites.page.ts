import { Component, OnInit, Input, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TournamentParticipant } from 'src/app/model/tournament-model';
import { CustomTournamentService } from 'src/app/services/custom-tournament.service';
import { TournamentSelectionService } from 'src/app/services/tournament-selection.service';
import { firstValueFrom } from 'rxjs';
import { IonicModule, ToastController, LoadingController, AlertController } from '@ionic/angular';
import { TranslateModule } from '@ngx-translate/core';


@Component({
  selector: 'app-pending-invites',
  standalone: true,
  imports: [CommonModule, IonicModule, TranslateModule],
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
    private alertController: AlertController
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
      message: 'Loading pending invites...',
      spinner: 'crescent'
    });
    await loading.present();

    try {
      this.pendingParticipants = await firstValueFrom(
        this.tournamentService.getTournamentParticipants(this.tournamentId, 'Invited')
      );
    } catch (error) {
      console.error('Error loading invited participants:', error);
      await this.showToast('Failed to load invites.', 'danger');
    } finally {
      this.isLoading = false;
      loading.dismiss();
    }
  }

  async confirmResendInvite(email: string) {
    const alert = await this.alertController.create({
      header: 'Resend Invitation',
      message: `Are you sure you want to resend the invitation to ${email}?`,
      buttons: [
        {
          text: 'Cancel',
          role: 'cancel'
        },
        {
          text: 'Resend',
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
      header: 'Exclude Participant',
      message: `Are you sure you want to exclude ${email} from the tournament?`,
      buttons: [
        {
          text: 'Cancel',
          role: 'cancel'
        },
        {
          text: 'Exclude',
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
          await this.showToast(result.message, 'success');
        } else {
          await this.showToast(result.message, 'danger');
        }
      },
      error: async (error) => {
        console.error('Error resending invite:', error);
        await this.showToast('Failed to resend invite.', 'danger');
      }
    });
  }
    
  excludeInvite(userEmail: string) {
    console.log('Confirming exclusion for:', userEmail);
  
    this.tournamentId = this.tournamentSelectionService.getSelectedTournament();
  
    this.tournamentService.excludeParticipant(this.tournamentId!, userEmail).subscribe({
      next: async (result) => {
        if (result.success) {
          await this.showToast(result.message, 'success');
          await this.loadPendingInvites(); // Refresh the list
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
