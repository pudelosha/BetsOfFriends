import { Component, Input, Output, EventEmitter, SimpleChanges, OnChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { IonProgressBar } from '@ionic/angular/standalone';
import { Router } from '@angular/router';
import { TournamentSelectionService } from 'src/app/services/tournament-selection.service';
import { CustomTournamentService } from 'src/app/services/custom-tournament.service';
import { SelectedTournamentDetails } from 'src/app/model/tournament-model';
import { firstValueFrom } from 'rxjs';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-selected-tournament',
  templateUrl: './selected-tournament.page.html',
  styleUrls: ['./selected-tournament.page.scss'],
  standalone: true,
  imports: [IonProgressBar, CommonModule, FormsModule, TranslateModule],
})
export class SelectedTournamentPage implements OnChanges {
  @Input() refreshTrigger: number = 0;
  @Output() loadingStart = new EventEmitter<void>();
  @Output() loadingEnd = new EventEmitter<void>();

  tournamentId: number | null = null;
  summaryData: SelectedTournamentDetails | null | undefined = null;
  isLoading = true;

  constructor(
    private router: Router,
    private tournamentSelectionService: TournamentSelectionService,
    private tournamentService: CustomTournamentService
  ) {}

  async ionViewWillEnter() {
    await this.loadTournamentAndFetchSummary();
  }

  async ngOnChanges(changes: SimpleChanges) {
    if (changes['refreshTrigger'] && !changes['refreshTrigger'].firstChange) {
      await this.loadTournamentAndFetchSummary();
    }
  }

  async loadTournamentAndFetchSummary() {
    this.loadingStart.emit();
    this.isLoading = true;

    this.tournamentId = this.tournamentSelectionService.getSelectedTournament();

    if (this.tournamentId === null) {
      this.isLoading = false;
      this.loadingEnd.emit();
      return;
    }

    try {
      this.summaryData = await firstValueFrom(
        this.tournamentService.getSelectedTournamentDetails(this.tournamentId)
      );
    } catch (error) {
      console.error('Failed to load tournament summary:', error);
    } finally {
      this.isLoading = false;
      this.loadingEnd.emit();
    }
  }

  goToMyTournaments() {
    this.router.navigate(['/my-tournaments']);
  }
}
