import { Component, OnInit, Input, Output, EventEmitter, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToastController } from '@ionic/angular';
import { Router } from '@angular/router';
import { TournamentSelectionService } from 'src/app/services/tournament-selection.service';
import { CustomMatchService } from 'src/app/services/custom-match.service';
import { firstValueFrom } from 'rxjs';
import { Match } from 'src/app/model/match'; // or your custom match model
import { TranslateModule } from '@ngx-translate/core';
import { IonSpinner, IonList, IonItem } from '@ionic/angular/standalone';

@Component({
  selector: 'app-started-custom-matches',
  standalone: true,
  imports: [CommonModule, TranslateModule, IonSpinner, IonList, IonItem],
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

  async ionViewWillEnter() {
    await this.loadTournamentAndFetchMatches(); // Refresh messages on page enter
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
      this.isLoading = false;
      this.loadingEnd.emit();
      return;
    }

    await this.loadStartedMatches();
    this.loadingEnd.emit();
  }

  async loadStartedMatches() {
    this.isLoading = true;
    this.startedMatches = [];
    this.errorMessage = '';
  
    if (this.tournamentId === null) {
      console.warn("No tournament ID provided.");
      this.errorMessage = "No tournament ID provided.";
      this.isLoading = false;
      return;
    }
  
    try {
      this.startedMatches = await firstValueFrom(
        this.matchService.getStartedMatches(this.tournamentId)
      );
  
      if (!this.startedMatches.length) {
        this.errorMessage = "No started matches found.";
      }
    } catch (error) {
      console.error("Error loading started custom matches:", error);
      this.errorMessage = "Failed to load matches.";
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
