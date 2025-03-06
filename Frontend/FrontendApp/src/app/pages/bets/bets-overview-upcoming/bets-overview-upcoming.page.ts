import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonicModule } from '@ionic/angular';
import { BetService } from 'src/app/services/bet.service';
import { TournamentSelectionService } from 'src/app/services/tournament-selection.service';
import { AggregatedBet, Bet } from 'src/app/model/bet';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-bets-overview-upcoming',
  templateUrl: './bets-overview-upcoming.page.html',
  styleUrls: ['./bets-overview-upcoming.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule],
})
export class BetsOverviewUpcomingPage implements OnInit {
  bets: AggregatedBet[] = [];
  isLoading = true;

  constructor(
    private betService: BetService,
    private tournamentSelectionService: TournamentSelectionService
  ) {}

  async ngOnInit() {
    await this.loadBets();
  }

  async loadBets() {
    this.isLoading = true;
    const tournamentId = this.tournamentSelectionService.getSelectedTournament();

    if (!tournamentId) {
      this.isLoading = false;
      return;
    }

    try {
      this.bets = await firstValueFrom(this.betService.getAggregatedBetsByStatus(tournamentId, 'Upcoming'));
    } catch (error) {
      console.error("Error loading upcoming bets:", error);
    } finally {
      this.isLoading = false;
    }
  }
}
