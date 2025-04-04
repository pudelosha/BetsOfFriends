import { Component, OnInit, Input, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonicModule, ToastController, LoadingController, AlertController } from '@ionic/angular';
import { firstValueFrom } from 'rxjs';
import { TournamentSelectionService } from 'src/app/services/tournament-selection.service';
import { CustomTournamentService } from 'src/app/services/custom-tournament.service';
import { TournamentParticipant } from 'src/app/model/tournament-model';
import { Router } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';


@Component({
  selector: 'app-user-list',
  templateUrl: './user-list.page.html',
  styleUrls: ['./user-list.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, TranslateModule],
})
export class UserListPage implements OnInit {
  @Input() refreshTrigger: number = 0;

  tournamentId: number | null = null;
  participants: TournamentParticipant[] = [];
  isLoading = true;

  constructor(
    private tournamentService: CustomTournamentService,
    private tournamentSelectionService: TournamentSelectionService,
    private toastController: ToastController,
    private loadingController: LoadingController,
    private router: Router,
    private alertController: AlertController
  ) {}

  async ngOnInit() {
    await this.loadTournamentAndFetchParticipants();
  }

  async ngOnChanges(changes: SimpleChanges) {
    if (changes['refreshTrigger'] && !changes['refreshTrigger'].firstChange) {
      this.loadTournamentAndFetchParticipants();
    }
  }

  private async loadTournamentAndFetchParticipants() {
    this.tournamentId = this.tournamentSelectionService.getSelectedTournament();

    if (this.tournamentId === null) {
      this.isLoading = false;
      return;
    }

    await this.loadParticipants();
  }

  async loadParticipants() {
    const loading = await this.loadingController.create({
      message: 'Loading participants...',
      spinner: 'crescent'
    });
    await loading.present();

    try {
      this.participants = await firstValueFrom(
        this.tournamentService.getTournamentParticipants(this.tournamentId!)
      );
    } catch (error) {
      console.error('Error loading participants:', error);
      await this.showToast('Failed to load participants.', 'danger');
    } finally {
      this.isLoading = false;
      await loading.dismiss();
    }
  }

  async confirmExclude(email: string) {
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
            this.excludeParticipant(email);
          }
        }
      ]
    });
    await alert.present();
  }

  async excludeParticipant(userEmail: string) {
    console.log('Confirming exclusion for:', userEmail);
  
    this.tournamentId = this.tournamentSelectionService.getSelectedTournament();
  
    this.tournamentService.excludeParticipant(this.tournamentId!, userEmail).subscribe({
      next: async (result) => {
        if (result.success) {
          await this.showToast(result.message, 'success');
          await this.loadParticipants(); // Refresh the list
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
  
  async showToast(message: string, color: 'success' | 'warning' | 'danger' | 'primary') {
    const toast = await this.toastController.create({
      message,
      duration: 3000,
      position: 'bottom',
      color
    });
    await toast.present();
  }
}