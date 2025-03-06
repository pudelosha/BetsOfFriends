import { Component, OnInit } from '@angular/core';
import { ModalController } from '@ionic/angular';
import { CommonModule } from '@angular/common';
import { IonicModule } from '@ionic/angular';
import { BetService } from 'src/app/services/bet.service';
import { TournamentSelectionService } from 'src/app/services/tournament-selection.service';
import { AggregatedBet, Bet } from 'src/app/model/bet';
import { firstValueFrom } from 'rxjs';
import { ParticipantsBetsModalComponent } from 'src/app/modals/participants-bets-modal/participants-bets-modal.component';

@Component({
  selector: 'app-bets-overview-finalised',
  templateUrl: './bets-overview-finalised.page.html',
  styleUrls: ['./bets-overview-finalised.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule],
})
export class BetsOverviewFinalisedPage implements OnInit {
  bets: AggregatedBet[] = [];
  isLoading = true;

  constructor(
    private modalCtrl: ModalController,
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
      this.bets = await firstValueFrom(this.betService.getAggregatedBetsByStatus(tournamentId, 'Finalised'));
    } catch (error) {
      console.error("Error loading finalised bets:", error);
    } finally {
      this.isLoading = false;
    }
  }

  async openParticipantsModal(matchId: number) {
    const modal = await this.modalCtrl.create({
      component: ParticipantsBetsModalComponent,
      componentProps: { matchId }
    });
    await modal.present();
  }
}
