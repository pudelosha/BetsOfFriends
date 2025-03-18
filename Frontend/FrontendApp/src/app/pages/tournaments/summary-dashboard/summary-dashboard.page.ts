import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonicModule, ToastController, LoadingController } from '@ionic/angular';
import { CustomTournamentService } from 'src/app/services/custom-tournament.service';
import { TournamentSelectionService } from 'src/app/services/tournament-selection.service';
import { TournamentSummary } from 'src/app/model/tournament-model';
import { ModalController } from '@ionic/angular';
import { PlayerStatsModalComponent } from 'src/app/modals/player-stats-modal/player-stats-modal.component';

@Component({
  selector: 'app-summary-dashboard',
  templateUrl: './summary-dashboard.page.html',
  styleUrls: ['./summary-dashboard.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule],
})
export class SummaryDashboardPage implements OnInit {
  tournamentId: number | null = null;
  summaryData: TournamentSummary[] = [];
  isLoading = true;

  constructor(
    private tournamentService: CustomTournamentService,
    private tournamentSelectionService: TournamentSelectionService,
    private toastController: ToastController,
    private loadingController: LoadingController,
    private modalController: ModalController
  ) {}

  async ngOnInit() {
    await this.loadTournamentAndFetchSummary();
  }

  async ionViewWillEnter() {
    await this.loadTournamentAndFetchSummary(); // Ensure summary is refreshed on view enter
  }

  private async loadTournamentAndFetchSummary() {
    this.tournamentId = this.tournamentSelectionService.getSelectedTournament();
    
    if (this.tournamentId === null) {
      await this.showToast('No tournament selected', 'warning');
      this.isLoading = false;
      return;
    }

    await this.fetchSummary();
  }

  async fetchSummary() {
    if (this.tournamentId === null) {
      console.error('Tournament ID is null, cannot fetch summary.');
      return;
    }
  
    const loading = await this.loadingController.create({
      message: 'Loading summary...',
      spinner: 'crescent',
    });
    await loading.present(); // Show spinner
  
    const startTime = Date.now(); // Capture start time
  
    this.tournamentService.getTournamentSummary(this.tournamentId).subscribe({
      next: async (summary) => {
        this.summaryData = summary;
        this.isLoading = false;
  
        const elapsedTime = Date.now() - startTime;
        const delay = Math.max(0, 500 - elapsedTime); // Ensure 500ms delay
  
        setTimeout(async () => {
          await loading.dismiss();
        }, delay);
      },
      error: async (error) => {
        console.error('Error fetching summary:', error);
        this.isLoading = false;
  
        const elapsedTime = Date.now() - startTime;
        const delay = Math.max(0, 500 - elapsedTime); // Ensure 500ms delay
  
        setTimeout(async () => {
          await loading.dismiss();
          await this.showToast('Error loading summary', 'danger');
        }, delay);
      },
    });
  }
  
  async openPlayerStats(userId: string) {
    const tournamentId = this.tournamentSelectionService.getSelectedTournament();
    
    if (!tournamentId) {
      console.error("No tournament selected.");
      return;
    }
  
    const modal = await this.modalController.create({
      component: PlayerStatsModalComponent,
      componentProps: { tournamentId, userId }
    });
  
    await modal.present();
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
