import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonicModule, ToastController, AlertController, LoadingController  } from '@ionic/angular';
import { CustomTournamentService } from 'src/app/services/custom-tournament.service';
import { Tournament } from 'src/app/model/tournament-model';
import { firstValueFrom } from 'rxjs';
import { Router } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';


@Component({
  selector: 'app-custom-tournaments-list',
  templateUrl: './custom-tournaments-list.page.html',
  styleUrls: ['./custom-tournaments-list.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, TranslateModule],
})
export class CustomTournamentsListPage implements OnInit {
  tournaments: Tournament[] = [];
  isLoading = true;

  constructor(
    private tournamentService: CustomTournamentService,
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
    const loading = await this.loadingController.create({
      message: 'Loading tournaments...',
      spinner: 'crescent',
    });
    await loading.present();
    
    const startTime = Date.now();

    this.tournamentService.getCustomTournaments().subscribe({
      next: (data) => {
        this.tournaments = data ?? [];
      },
      error: (err) => {
        console.error('Error loading tournaments:', err);
        this.tournaments = [];
        this.showToast('Failed to load tournaments.', 'danger');
      },
      complete: async () => {
        const elapsedTime = Date.now() - startTime;
        const delay = Math.max(0, 500 - elapsedTime);
        setTimeout(() => loading.dismiss(), delay);
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

  async recalculateBets(tournament: any) {
    const loading = await this.loadingController.create({
      message: 'Recalculating bets...',
      spinner: 'crescent',
    });
    await loading.present();

    const startTime = Date.now();

    try {
      await firstValueFrom(this.tournamentService.recalculateBetsForTournament(tournament.tournamentId));
      this.showToast('Bets recalculated successfully!', 'success');
    } catch (error) {
      this.showToast('Error recalculating bets!', 'danger');
      console.error(error);
    } finally {
      const elapsedTime = Date.now() - startTime;
      const delay = Math.max(0, 500 - elapsedTime);
      setTimeout(() => loading.dismiss(), delay);
    }
  }
  
  async toggleTournamentStatus(tournament: any) {
    const newStatus = !tournament.isActive;
    
    const loading = await this.loadingController.create({
      message: `Updating status...`,
      spinner: 'crescent',
    });
    await loading.present();

    const startTime = Date.now();

    try {
      await this.tournamentService.updateCustomTournamentStatus(tournament.tournamentId, newStatus).toPromise();
      tournament.isActive = newStatus;
      this.showToast(`Tournament ${newStatus ? 'enabled' : 'disabled'} successfully!`, 'success');
    } catch (error) {
      tournament.isActive = !newStatus;
      console.error('Error toggling tournament status:', error);
      this.showToast('Failed to toggle tournament status. Please try again.', 'danger');
    } finally {
      const elapsedTime = Date.now() - startTime;
      const delay = Math.max(0, 500 - elapsedTime);
      setTimeout(() => loading.dismiss(), delay);
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
                        await firstValueFrom(this.tournamentService.deleteCustomTournament(tournament.tournamentId));

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
