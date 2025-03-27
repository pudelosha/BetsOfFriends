import { Component, OnInit, Input, Output, EventEmitter, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonicModule, ToastController } from '@ionic/angular';
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
  @Input() refreshTrigger: number = 0;
  @Output() loadingStart = new EventEmitter<void>();
  @Output() loadingEnd = new EventEmitter<void>();

  tournamentId: number | null = null;
  startedMatches: Match[] = [];
  isLoading = true;
  errorMessage: string | null = null;

  constructor(
    private matchService: CustomMatchService,
    private router: Router,
    private tournamentSelectionService: TournamentSelectionService,
    private toastController: ToastController
  ) {}

  async ngOnInit() {
    await this.loadTournamentAndFetchMatches();
  }

  async ngOnChanges(changes: SimpleChanges) {
    if (changes['refreshTrigger'] && !changes['refreshTrigger'].firstChange) {
      this.loadTournamentAndFetchMatches();
    }
  }

  private async loadTournamentAndFetchMatches() {
    this.loadingStart.emit();
    this.tournamentId = this.tournamentSelectionService.getSelectedTournament();

    if (this.tournamentId === null) {
      await this.showToast('No tournament selected', 'warning');
      this.isLoading = false;
      this.loadingEnd.emit();
      return;
    }

    await this.loadStartedMatches();
    this.loadingEnd.emit();
  }

  async loadStartedMatches() {
    if (this.tournamentId === null) return;

    try {
      this.startedMatches = await firstValueFrom(this.matchService.getStartedMatches(this.tournamentId));
    } catch (error) {
      console.error('Error loading started custom matches:', error);
      this.errorMessage = 'Failed to load matches.';
    } finally {
      this.isLoading = false;
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
    this.router.navigate(['/matches/custom'], {
      queryParams: { tab: 'started' }
    });
  }
}
