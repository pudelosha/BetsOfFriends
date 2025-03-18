import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TournamentSelectionService } from 'src/app/services/tournament-selection.service';
import { UpcomingBet } from 'src/app/model/bet';
import { firstValueFrom } from 'rxjs';
import { IonicModule, ToastController, LoadingController } from '@ionic/angular';
import { BetService } from 'src/app/services/bet.service';
import { Router } from '@angular/router';


@Component({
  selector: 'app-upcoming-bets',
  standalone: true,
  imports: [CommonModule, IonicModule],
  templateUrl: './upcoming-bets.page.html',
  styleUrls: ['./upcoming-bets.page.scss']
})
export class UpcomingBetsPage implements OnInit {
  tournamentId: number | null = null;
  upcomingGames: UpcomingBet[] = [];
  isLoading = true;
  errorMessage: string | null = null;

  constructor(
    private betService: BetService,
    private router: Router,
    private tournamentSelectionService: TournamentSelectionService,
    private toastController: ToastController,
    private loadingController: LoadingController
  ) {}

  async ngOnInit() {
    await this.loadTournamentAndFetchBets();
  }

  async ionViewWillEnter() {
    await this.loadTournamentAndFetchBets(); // Ensure upcoming bets refresh when view is re-entered
  }

  private async loadTournamentAndFetchBets() {
    this.tournamentId = this.tournamentSelectionService.getSelectedTournament();

    if (this.tournamentId === null) {
      await this.showToast('No tournament selected', 'warning');
      this.isLoading = false;
      return;
    }

    await this.loadUpcomingBets();
  }

  async loadUpcomingBets() {
    if (this.tournamentId === null) {
      console.error('Tournament ID is null, cannot fetch upcoming bets.');
      return;
    }

    const loading = await this.loadingController.create({
      message: 'Loading upcoming bets...',
      spinner: 'crescent',
      cssClass: 'custom-loading-spinner'
    });
    await loading.present();

    try {
      this.upcomingGames = await firstValueFrom(this.betService.getUpcomingBets(this.tournamentId));
    } catch (error) {
      console.error('Error fetching upcoming bets:', error);
      this.errorMessage = 'Failed to load upcoming bets.';
    } finally {
      this.isLoading = false;
      loading.dismiss();
    }
  }

  goToMyBets(){
    this.router.navigate(['/my-bets']);
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
