import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TournamentInvite } from 'src/app/model/tournament-model';
import { firstValueFrom } from 'rxjs';
import { IonicModule, ToastController, LoadingController } from '@ionic/angular';
import { CustomTournamentService } from 'src/app/services/custom-tournament.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-tournament-invites',
  standalone: true,
  imports: [CommonModule, IonicModule],
  templateUrl: './tournament-invites.page.html',
  styleUrls: ['./tournament-invites.page.scss']
})
export class TournamentInvitesPage implements OnInit {
  invites: TournamentInvite[] = [];
  isLoading = true;
  errorMessage: string | null = null;

  constructor(
    private tournamentService: CustomTournamentService,
    private toastController: ToastController,
    private router: Router,
    private loadingController: LoadingController
  ) {}

  async ngOnInit() {
    await this.loadTournamentInvites();
  }

  private async loadTournamentInvites() {
    const loading = await this.loadingController.create({
      message: 'Loading tournament invites...',
      spinner: 'crescent',
    });
    await loading.present();

    try {
      this.invites = await firstValueFrom(this.tournamentService.getPendingTournamentInvites());

      // Hide component if no invites
      if (this.invites.length === 0) {
        this.invites = [];
      }
    } catch (error) {
      console.error('Error fetching tournament invites:', error);
      this.errorMessage = 'Failed to load tournament invites.';
    } finally {
      this.isLoading = false;
      loading.dismiss();
    }
  }

  goToMyTournaments() {
    this.router.navigate(['/my-tournaments']);
  }

  async showToast(message: string, color: 'success' | 'warning' | 'danger' | 'primary') {
    const toast = await this.toastController.create({
      message,
      duration: 3000,
      position: 'bottom',
      color,
    });
    await toast.present();
  }
}
