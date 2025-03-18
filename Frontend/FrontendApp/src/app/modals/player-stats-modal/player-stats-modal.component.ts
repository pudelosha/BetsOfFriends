import { Component, Input, OnInit } from '@angular/core';
import { IonicModule, ModalController } from '@ionic/angular';
import { CommonModule } from '@angular/common';
import { CustomTournamentService } from 'src/app/services/custom-tournament.service';
import { UserBettingStats } from 'src/app/model/tournament-model';

@Component({
  selector: 'app-player-stats-modal',
  templateUrl: './player-stats-modal.component.html',
  styleUrls: ['./player-stats-modal.component.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule],
})
export class PlayerStatsModalComponent implements OnInit {
  @Input() tournamentId!: number;
  @Input() userId!: string;

  isLoading = true;
  stats: UserBettingStats[] = [];

  constructor(
    private modalController: ModalController,
    private tournamentService: CustomTournamentService
  ) {}

  ngOnInit() {
    this.fetchUserStats();
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
}
