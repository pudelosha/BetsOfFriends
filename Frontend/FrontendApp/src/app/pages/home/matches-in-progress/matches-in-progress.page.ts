import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { TranslateModule } from '@ngx-translate/core';
import { IonList, IonItem } from '@ionic/angular/standalone';
import { Bet } from 'src/app/model/bet';
import { BetService } from 'src/app/services/bet.service';
import { TournamentSelectionService } from 'src/app/services/tournament-selection.service';

@Component({
  selector: 'app-matches-in-progress',
  standalone: true,
  imports: [CommonModule, TranslateModule, IonList, IonItem],
  templateUrl: './matches-in-progress.page.html',
  styleUrls: ['./matches-in-progress.page.scss']
})
export class MatchesInProgressPage implements OnChanges {
  @Input() refreshTrigger: number = 0;
  @Output() loadingStart = new EventEmitter<void>();
  @Output() loadingEnd = new EventEmitter<void>();

  matches: Bet[] = [];
  isLoading = true;

  constructor(
    private betService: BetService,
    private tournamentSelectionService: TournamentSelectionService,
    private router: Router
  ) {}

  async ngOnChanges(changes: SimpleChanges) {
    if (changes['refreshTrigger'] && !changes['refreshTrigger'].firstChange) {
      this.loadMatchesInProgress();
    }
  }

  async ionViewWillEnter() {
    await this.loadMatchesInProgress();
  }

  async loadMatchesInProgress() {
    this.loadingStart.emit();
    this.isLoading = true;
    this.matches = [];

    try {
      const tournamentId = this.tournamentSelectionService.getSelectedTournament();

      if (tournamentId === null) {
        return;
      }

      this.matches = await firstValueFrom(this.betService.getInProgressBets(tournamentId));
    } catch (error) {
      console.error('Error fetching matches in progress:', error);
    } finally {
      this.isLoading = false;
      this.loadingEnd.emit();
    }
  }

  formatLiveResult(match: Bet): string {
    return this.formatScore(match.actualHomeGoals, match.actualAwayGoals);
  }

  formatMyBet(match: Bet): string {
    return this.formatScore(match.playerHomeGoals, match.playerAwayGoals);
  }

  goToClosedBets() {
    this.router.navigate(['/my-bets'], {
      queryParams: {
        tab: 'finalised'
      }
    });
  }

  private formatScore(homeGoals?: number | null, awayGoals?: number | null): string {
    const home = homeGoals ?? '-';
    const away = awayGoals ?? '-';
    return `${home}-${away}`;
  }
}
