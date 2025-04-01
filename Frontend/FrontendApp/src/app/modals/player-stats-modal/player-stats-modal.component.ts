import { Component, Input, OnInit } from '@angular/core';
import { IonicModule, ModalController } from '@ionic/angular';
import { CommonModule } from '@angular/common';
import { CustomTournamentService } from 'src/app/services/custom-tournament.service';
import { UserBettingStats } from 'src/app/model/tournament-model';
import { TranslateModule } from '@ngx-translate/core';


@Component({
  selector: 'app-player-stats-modal',
  templateUrl: './player-stats-modal.component.html',
  styleUrls: ['./player-stats-modal.component.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, TranslateModule],
})
export class PlayerStatsModalComponent implements OnInit {
  @Input() tournamentId!: number;
  @Input() userId!: string;

  isLoading = true;
  stats: UserBettingStats[] = [];
  expandedMatchId: number | null = null;

  constructor(
    private modalController: ModalController,
    private tournamentService: CustomTournamentService
  ) {}

  ngOnInit() {
    this.fetchUserStats();
  }

  onAccordionChange(event: Event) {
    const customEvent = event as CustomEvent;
    const expandedMatchId = customEvent.detail?.value;  
    this.expandedMatchId = expandedMatchId !== undefined ? Number(expandedMatchId) : null;
  }
      
  fetchUserStats() {
    this.tournamentService.getUserBettingStats(this.tournamentId, this.userId).subscribe({
      next: (data) => {
        this.stats = data;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error fetching player stats:', error);
        this.isLoading = false;
      }
    });
  }

  closeModal() {
    this.modalController.dismiss();
  }

  getStatusIcon(value?: string): string {
    return value === 'V' ? '✔' : value === 'X' ? '✘' : '-';
  }
  
  getStatusClass(value?: string): string {
    return value === 'V' ? 'v-status' : value === 'X' ? 'x-status' : '';
  }  
}
