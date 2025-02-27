import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonicModule, ToastController, AlertController } from '@ionic/angular';
import { CustomTournamentService } from 'src/app/services/custom-tournament.service';
import { Tournament } from 'src/app/model/tournament-model';
import { firstValueFrom } from 'rxjs';
import { Router } from '@angular/router';

@Component({
  selector: 'app-custom-tournaments-list',
  templateUrl: './custom-tournaments-list.page.html',
  styleUrls: ['./custom-tournaments-list.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule],
})
export class CustomTournamentsListPage implements OnInit {
  tournaments: Tournament[] = []; // Store fetched tournaments
  isLoading = true; // Loader state

  constructor(
    private tournamentService: CustomTournamentService,
    private toastController: ToastController,
    private alertController: AlertController,
    private router: Router
  ) {}

  ngOnInit() {
    this.loadTournaments();
  }

  ionViewWillEnter() {
    this.loadTournaments();
    this.scrollToTop();
  }

  private scrollToTop(): void {
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  loadTournaments() {
    this.isLoading = true;
    this.tournamentService.getCustomTournaments().subscribe({
      next: (data) => {
        this.tournaments = data ?? [];
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Error loading tournaments:', err);
        this.tournaments = [];
        this.isLoading = false;
      }
    });
  }

  editTournament(tournament: Tournament): void {
    if (!tournament || !tournament.tournamentId) {
      console.error('Invalid tournament object:', tournament);
      this.showToast('Invalid tournament data. Unable to edit.', 'danger');
      return;
    }

    this.router.navigate([`/tournaments/update-custom/${tournament.tournamentId}`]).catch((error) => {
      console.error('Navigation to edit tournament failed:', error);
      this.showToast('Failed to navigate to the tournament editor.', 'danger');
    });
  }

  async toggleTournamentStatus(tournament: any) {
    const newStatus = !tournament.isActive;
  
    try {
      await this.tournamentService.updateCustomTournamentStatus(tournament.tournamentId, newStatus).toPromise();
      tournament.isActive = newStatus;
      this.showToast(`Tournament ${newStatus ? 'enabled' : 'disabled'} successfully!`, 'success');
    } catch (error) {
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
      await firstValueFrom(this.tournamentService.deleteCustomTournament(tournament.tournamentId));
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
