import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonicModule, ToastController, AlertController, LoadingController } from '@ionic/angular';
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
    private router: Router,
    private loadingController: LoadingController
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

  async loadTournaments() {
    this.isLoading = true;

    const loading = await this.loadingController.create({
      message: 'Loading tournaments...',
      spinner: 'crescent',
    });
    await loading.present();

    const startTime = Date.now();

    try {
      this.tournaments = await firstValueFrom(this.tournamentService.getPredefinedTournaments());
    } catch (err) {
      console.error('Error loading tournaments:', err);
      this.tournaments = [];
    } finally {
      const elapsedTime = Date.now() - startTime;
      const delay = Math.max(0, 500 - elapsedTime);

      setTimeout(async () => {
        this.isLoading = false;
        await loading.dismiss();
      }, delay);
    }
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

  editMatches(tournamentId: number | null | undefined): void {
    if (tournamentId == null) {
      console.warn('Invalid tournament ID');
      return;
    }
  
    this.router.navigate(['/matches/predefined', tournamentId]);
  }  
  
  async toggleTournamentStatus(tournament: any) {
    const newStatus = !tournament.isActive;

    const loading = await this.loadingController.create({
      message: `Updating tournament...`,
      spinner: 'crescent',
    });
    await loading.present();

    const startTime = Date.now();

    try {
      await this.tournamentService.updatePredefinedTournamentStatus(tournament.tournamentId, newStatus).toPromise();
      tournament.isActive = newStatus;
      this.showToast(`Tournament ${newStatus ? 'enabled' : 'disabled'} successfully!`, 'success');
    } catch (error) {
      tournament.isActive = !newStatus; // Revert UI on error
      console.error('Error toggling tournament status:', error);
      this.showToast('Failed to toggle tournament status. Please try again.', 'danger');
    } finally {
      const elapsedTime = Date.now() - startTime;
      const delay = Math.max(0, 500 - elapsedTime);

      setTimeout(async () => {
        await loading.dismiss();
      }, delay);
    }
  }

  async deleteTournament(tournament: any) {
    const alert = await this.alertController.create({
        header: 'Confirm Deletion',
        message: `Are you sure you want to delete ${tournament.tournamentName}?`,
        buttons: [
            { text: 'Cancel', role: 'cancel' },
            {
                text: 'Delete',
                handler: async () => {
                    const loading = await this.loadingController.create({
                        message: `Deleting tournament...`,
                        spinner: 'crescent',
                    });
                    await loading.present();

                    const startTime = Date.now();

                    try {
                        // Call backend API to delete the tournament
                        await firstValueFrom(this.tournamentService.deletePredefinedTournament(tournament.tournamentId));

                        // Remove the deleted tournament from the local list instantly
                        this.tournaments = this.tournaments.filter(t => t.tournamentId !== tournament.tournamentId);

                        this.showToast('Tournament deleted successfully!', 'success');
                    } catch (error) {
                        this.showToast('Error deleting tournament!', 'danger');
                        console.error(error);
                    } finally {
                        const elapsedTime = Date.now() - startTime;
                        const delay = Math.max(0, 500 - elapsedTime);

                        setTimeout(async () => {
                            await loading.dismiss();
                        }, delay);
                    }
                }
            }
        ]
    });

    await alert.present();
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
