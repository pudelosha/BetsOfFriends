import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { TranslateModule } from '@ngx-translate/core';
import { IonList, IonItem } from '@ionic/angular/standalone';
import { BetService } from 'src/app/services/bet.service';
import { TournamentSelectionService } from 'src/app/services/tournament-selection.service';
import { MissingBetMatch } from 'src/app/model/bet';

@Component({
  selector: 'app-missing-bets',
  standalone: true,
  imports: [CommonModule, TranslateModule, IonList, IonItem],
  templateUrl: './missing-bets.page.html',
  styleUrls: ['./missing-bets.page.scss']
})
export class MissingBetsPage implements OnChanges {
  @Input() refreshTrigger: number = 0;
  @Output() loadingStart = new EventEmitter<void>();
  @Output() loadingEnd = new EventEmitter<void>();

  tournamentId: number | null = null;
  matches: MissingBetMatch[] = [];
  isLoading = true;
  visible = false;

  constructor(
    private betService: BetService,
    private tournamentSelectionService: TournamentSelectionService,
    private router: Router
  ) {}

  async ngOnChanges(changes: SimpleChanges) {
    if (changes['refreshTrigger'] && !changes['refreshTrigger'].firstChange) {
      this.loadMissingBets();
    }
  }

  async ionViewWillEnter() {
    await this.loadMissingBets();
  }

  async loadMissingBets() {
    this.loadingStart.emit();
    this.isLoading = true;
    this.visible = false;
    this.matches = [];

    try {
      this.tournamentId = this.tournamentSelectionService.getSelectedTournament();

      if (this.tournamentId === null) {
        return;
      }

      const summary = await firstValueFrom(this.betService.getMissingBets(this.tournamentId));
      this.matches = summary.matches ?? [];
      this.visible = summary.canView && this.matches.length > 0;
    } catch (error) {
      if (error instanceof HttpErrorResponse && (error.status === 403 || error.status === 404)) {
        return;
      }

      console.error('Error fetching missing bets:', error);
    } finally {
      this.isLoading = false;
      this.loadingEnd.emit();
    }
  }

  getParticipantNames(match: MissingBetMatch): string {
    return match.participants
      .map(participant => participant.userName)
      .join(', ');
  }

  goToPlacedBets() {
    this.router.navigate(['/my-bets'], {
      queryParams: {
        tab: 'placed'
      }
    });
  }
}
