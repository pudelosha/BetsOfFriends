import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonicModule, ToastController, LoadingController } from '@ionic/angular';
import { Router } from '@angular/router';
import { TournamentSelectionService } from 'src/app/services/tournament-selection.service';
import { CustomMatchService } from 'src/app/services/custom-match.service';
import { firstValueFrom } from 'rxjs';
import { Match } from 'src/app/model/match'; // or your custom match model

@Component({
  selector: 'app-started-custom-matches',
  standalone: true,
  imports: [CommonModule, IonicModule],
  templateUrl: './started-custom-matches.page.html',
  styleUrls: ['./started-custom-matches.page.scss']
})
export class StartedCustomMatchesPage implements OnInit {
  tournamentId: number | null = null;
  startedMatches: Match[] = [];
  isLoading = true;
  errorMessage: string | null = null;

  constructor(
    private matchService: CustomMatchService,
    private router: Router,
    private tournamentSelectionService: TournamentSelectionService,
    private toastController: ToastController,
    private loadingController: LoadingController
  ) {}

  async ngOnInit() {
    await this.loadTournamentAndFetchMatches();
  }

  private async loadTournamentAndFetchMatches() {
    this.tournamentId = this.tournamentSelectionService.getSelectedTournament();

    if (this.tournamentId === null) {
      await this.showToast('No tournament selected', 'warning');
      this.isLoading = false;
      return;
    }

    await this.loadStartedMatches();
  }

  async loadStartedMatches() {
    if (this.tournamentId === null) return;

    const loading = await this.loadingController.create({
      message: 'Loading started matches...',
      spinner: 'crescent',
      cssClass: 'custom-loading-spinner'
    });
    await loading.present();

    try {
      this.startedMatches = await firstValueFrom(this.matchService.getStartedMatches(this.tournamentId));
    } catch (error) {
      console.error('Error loading started custom matches:', error);
      this.errorMessage = 'Failed to load matches.';
    } finally {
      this.isLoading = false;
      loading.dismiss();
    }
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

  navigateToCustomMatches() {
    this.router.navigate(['/matches/custom']);
  }
}
