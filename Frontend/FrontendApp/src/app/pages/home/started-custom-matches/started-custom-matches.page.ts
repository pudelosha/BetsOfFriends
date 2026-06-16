import { Component, Input, Output, EventEmitter, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToastController } from '@ionic/angular';
import { Router } from '@angular/router';
import { TournamentSelectionService } from 'src/app/services/tournament-selection.service';
import { CustomMatchService } from 'src/app/services/custom-match.service';
import { CustomTournamentService } from 'src/app/services/custom-tournament.service';
import { firstValueFrom } from 'rxjs';
import { Match } from 'src/app/model/match';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { IonList, IonItem } from '@ionic/angular/standalone';

@Component({
  selector: 'app-started-custom-matches',
  standalone: true,
  imports: [CommonModule, TranslateModule, IonList, IonItem],
  templateUrl: './started-custom-matches.page.html',
  styleUrls: ['./started-custom-matches.page.scss']
})
export class StartedCustomMatchesPage implements OnChanges {
  @Input() refreshTrigger: number = 0;
  @Output() loadingStart = new EventEmitter<void>();
  @Output() loadingEnd = new EventEmitter<void>();

  tournamentId: number | null = null;
  startedMatches: Match[] = [];
  isLoading = true;
  isAutoUpdateTournament = false;
  errorMessage: string | null = null;

  constructor(
    private matchService: CustomMatchService,
    private tournamentService: CustomTournamentService,
    private router: Router,
    private tournamentSelectionService: TournamentSelectionService,
    private toastController: ToastController,
    private translate: TranslateService
  ) {}

  async ionViewWillEnter() {
    await this.loadTournamentAndFetchMatches();
  }

  async ngOnChanges(changes: SimpleChanges) {
    if (changes['refreshTrigger'] && !changes['refreshTrigger'].firstChange) {
      this.loadTournamentAndFetchMatches();
    }
  }

  private async loadTournamentAndFetchMatches() {
    this.loadingStart.emit();
    this.isLoading = true;

    try {
      this.tournamentId = this.tournamentSelectionService.getSelectedTournament();
      this.isAutoUpdateTournament = false;

      if (this.tournamentId === null) {
        return;
      }

      const tournamentDetails = await firstValueFrom(
        this.tournamentService.getSelectedTournamentDetails(this.tournamentId)
      );
      this.isAutoUpdateTournament = tournamentDetails.updateMethod === 'Auto';

      if (this.isAutoUpdateTournament) {
        this.startedMatches = [];
        return;
      }

      await this.loadStartedMatches();
    } finally {
      this.isLoading = false;
      this.loadingEnd.emit();
    }
  }

  async loadStartedMatches() {
    this.isLoading = true;
    this.startedMatches = [];
    this.errorMessage = '';
  
    if (this.tournamentId === null) {
      console.warn("No tournament ID provided.");
      this.errorMessage = this.t('TOASTS.NO_TOURNAMENT_ID');
      this.isLoading = false;
      return;
    }
  
    try {
      this.startedMatches = await firstValueFrom(
        this.matchService.getStartedMatches(this.tournamentId)
      );
  
      if (!this.startedMatches.length) {
        this.errorMessage = this.t('TOASTS.NO_STARTED_MATCHES');
      }
    } catch (error) {
      console.error("Error loading started custom matches:", error);
      this.errorMessage = this.t('TOASTS.MATCHES_LOAD_FAILED');
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

  private t(key: string, params?: Record<string, unknown>): string {
    return this.translate.instant(key, params);
  }

  navigateToCustomMatches() {
    if (this.isAutoUpdateTournament) {
      return;
    }

    this.router.navigate(['/matches/custom'], {
      queryParams: { tab: 'started' }
    });
  }
}
