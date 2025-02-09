import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonicModule, ToastController, AlertController } from '@ionic/angular';
import { PredefinedTournamentService } from '../../../services/predefined-tournament.service';
import { Tournament } from '../../../model/tournament-model';

@Component({
  selector: 'app-predefined-tournaments',
  templateUrl: './predefined-tournaments.page.html',
  styleUrls: ['./predefined-tournaments.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule],
})
export class PredefinedTournamentsPage implements OnInit {
  tournaments: Tournament[] = []; // Store fetched tournaments
  isLoading = true; // Loader state

  constructor(
    private tournamentService: PredefinedTournamentService,
    private toastController: ToastController,
    private alertController: AlertController
  ) {}

  ngOnInit() {
    // Optional: Load once when component initializes
    this.loadTournaments();
  }

  ionViewWillEnter() {
    // Refresh tournaments every time the page is visited
    this.loadTournaments();
  }

  loadTournaments() {
    this.isLoading = true; // Start loading
    this.tournamentService.getPredefinedTournaments().subscribe({
      next: (data) => {
        this.tournaments = data ?? [];
        this.isLoading = false; // Stop loading after data is fetched
      },
      error: (err) => {
        console.error('Error loading tournaments:', err);
        this.tournaments = [];
        this.isLoading = false; // Ensure loading stops even if an error occurs
      }
    });
  }
  

  editTournament(tournament: any) {
    window.location.href = `/create-predefined-tournament/${tournament.id}`;
  }

  async toggleTournamentStatus(tournament: any) {
    const newStatus = !tournament.isActive;
    tournament.isActive = newStatus;
    await this.tournamentService.updatePredefinedTournamentStatus(tournament.id, newStatus).toPromise();
    this.showToast(`Tournament ${newStatus ? 'enabled' : 'disabled'} successfully!`, 'success');
  }

  async confirmDelete(tournament: any) {
    const alert = await this.alertController.create({
      header: 'Confirm Deletion',
      message: `Are you sure you want to delete ${tournament.tournamentName}?`,
      buttons: [
        { text: 'Cancel', role: 'cancel' },
        {
          text: 'Delete',
          handler: async () => {
            await this.deleteTournament(tournament);
          },
        },
      ],
    });
    await alert.present();
  }

  async deleteTournament(tournament: any) {
    try {
      await this.tournamentService.deletePredefinedTournament(tournament.id).toPromise();
      this.tournaments = this.tournaments.filter(t => t.tournamentId !== tournament.id);
      this.showToast('Tournament deleted successfully!', 'success');
    } catch (error) {
      this.showToast('Error deleting tournament!', 'danger');
      console.error(error);
    }
  }

  async showToast(message: string, color: 'success' | 'warning' | 'danger') {
    const toast = await this.toastController.create({
      message,
      duration: 3000,
      position: 'bottom',
      color,
    });
    await toast.present();
  }
}
