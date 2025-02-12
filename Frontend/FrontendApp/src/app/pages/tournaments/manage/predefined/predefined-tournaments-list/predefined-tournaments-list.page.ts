import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonicModule, ToastController, AlertController } from '@ionic/angular';
import { PredefinedTournamentService } from 'src/app/services/predefined-tournament.service';
import { Tournament } from 'src/app/model/tournament-model';
import { firstValueFrom } from 'rxjs';
import { Router } from '@angular/router';

@Component({
  selector: 'app-predefined-tournaments-list',
  templateUrl: './predefined-tournaments-list.page.html',
  styleUrls: ['./predefined-tournaments-list.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule],
})
export class PredefinedTournamentsListPage implements OnInit {
  tournaments: Tournament[] = []; // Store fetched tournaments
  isLoading = true; // Loader state

  constructor(
    private tournamentService: PredefinedTournamentService,
    private toastController: ToastController,
    private alertController: AlertController,
    private router: Router
  ) {}

  ngOnInit() {
    // Optional: Load once when component initializes
    this.loadTournaments();
  }

  ionViewWillEnter() {
    // Refresh tournaments every time the page is visited
    this.loadTournaments();
    this.scrollToTop();
  }

  private scrollToTop(): void {
    window.scrollTo({ top: 0, behavior: 'smooth' });
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
  

  editTournament(tournament: Tournament): void {
    if (!tournament || !tournament.tournamentId) {
      console.error('Invalid tournament object:', tournament);
      this.showToast('Invalid tournament data. Unable to edit.', 'danger');
      return;
    }

    console.log('Navigating to edit tournament with ID:', tournament.tournamentId);

    this.router.navigate([`/tournaments/update-predefined/${tournament.tournamentId}`]).catch((error) => {
      console.error('Navigation to edit tournament failed:', error);
      this.showToast('Failed to navigate to the tournament editor.', 'danger');
    });
  }
  
  async toggleTournamentStatus(tournament: any) {
    const newStatus = !tournament.isActive;
  
    try {
      // Call the backend to update the status
      await this.tournamentService.updatePredefinedTournamentStatus(tournament.tournamentId, newStatus).toPromise();
  
      // Update the frontend state only after a successful API call
      tournament.isActive = newStatus;
      this.showToast(`Tournament ${newStatus ? 'enabled' : 'disabled'} successfully!`, 'success');
    } catch (error) {
      // Revert the status change on the frontend in case of an error
      tournament.isActive = !newStatus;
      console.error('Error toggling tournament status:', error);
      this.showToast('Failed to toggle tournament status. Please try again.', 'danger');
    }
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
            try {
              await this.deleteTournament(tournament);
              this.showToast(`${tournament.tournamentName} has been deleted successfully!`, 'success');
  
              // Optionally, refresh the list of tournaments
              this.loadTournaments();
            } catch (error) {
              console.error('Error deleting tournament:', error);
              this.showToast('Failed to delete tournament. Please try again.', 'danger');
            }
          },
        },
      ],
    });
    await alert.present();
  }
    
  async deleteTournament(tournament: any) {
    try {
      await firstValueFrom(this.tournamentService.deletePredefinedTournament(tournament.tournamentId));
      this.tournaments = this.tournaments.filter(t => t.tournamentId !== tournament.tournamentId);
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
