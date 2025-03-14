import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TournamentSelectionService } from 'src/app/services/tournament-selection.service';
import { CustomTournamentService } from 'src/app/services/custom-tournament.service';
import { TournamentPlayerResult } from 'src/app/model/tournament-model';
import { firstValueFrom } from 'rxjs';
import { IonicModule, ToastController, LoadingController } from '@ionic/angular';
import { Router } from '@angular/router';

@Component({
  selector: 'app-tournament-summary',
  standalone: true,
  imports: [CommonModule, IonicModule],
  templateUrl: './tournament-summary.page.html',
  styleUrls: ['./tournament-summary.page.scss']
})
export class TournamentSummaryPage implements OnInit {
  tournamentId: number | null = null;
  players: TournamentPlayerResult[] = [];
  isLoading = true;

  constructor(
    private tournamentService: CustomTournamentService,
    private tournamentSelectionService: TournamentSelectionService,
    private router: Router,
    private toastController: ToastController,
    private loadingController: LoadingController
  ) {}

  async ngOnInit() {
    await this.loadTournamentAndFetchSummary();
  }

  async ionViewWillEnter() {
    await this.loadTournamentAndFetchSummary(); // Ensure summary refresh when view is re-entered
  }

  private async loadTournamentAndFetchSummary() {
    this.tournamentId = this.tournamentSelectionService.getSelectedTournament();

    if (this.tournamentId === null) {
      await this.showToast('No tournament selected', 'warning');
      this.isLoading = false;
      return;
    }

    await this.loadTournamentSummary();
  }

  async loadTournamentSummary() {
    if (this.tournamentId === null) {
      console.error('Tournament ID is null, cannot fetch summary.');
      return;
    }

    const loading = await this.loadingController.create({
      message: 'Loading summary...',
      spinner: 'crescent',
    });
    await loading.present();

    try {
      this.players = await firstValueFrom(this.tournamentService.getTournamentPlayerResult(this.tournamentId));
    } catch (error) {
      console.error('Error fetching summary:', error);
      await this.showToast('Failed to load tournament summary', 'danger');
    } finally {
      this.isLoading = false;
      loading.dismiss();
    }
  }

  goToMyBets() {
    this.router.navigate(['/summary']);
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
