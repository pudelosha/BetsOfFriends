import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonicModule, ToastController, ModalController } from '@ionic/angular';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { BetService } from 'src/app/services/bet.service';
import { TournamentSelectionService } from 'src/app/services/tournament-selection.service';
import { Bet, BetStats } from 'src/app/model/bet';
import { firstValueFrom } from 'rxjs';
import { BetsOverviewModalComponent } from 'src/app/modals/bets-overview-modal/bets-overview-modal.component';

@Component({
  selector: 'app-my-bets-finalised',
  templateUrl: './my-bets-finalised.page.html',
  styleUrls: ['./my-bets-finalised.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule, FormsModule],
})
export class MyBetsFinalisedPage implements OnInit {
  bets: Bet[] = [];
  isLoading = true;

  constructor(
    private modalCtrl: ModalController,
    private betService: BetService,
    private tournamentSelectionService: TournamentSelectionService,
    private toastController: ToastController
  ) {}

  async ngOnInit() {
    await this.loadBets();
  }

  async loadBets() {
    this.isLoading = true;
    const tournamentId = this.tournamentSelectionService.getSelectedTournament();

    if (!tournamentId) {
      this.showToast('No tournament selected!', 'warning');
      this.isLoading = false;
      return;
    }

    try {
      this.bets = await firstValueFrom(this.betService.getBetsByStatus(tournamentId, 'Finalised'));
      console.log('Loaded Finalised Bets:', this.bets);
    } catch (error) {
      console.error('Error fetching finalised bets:', error);
      this.showToast('Failed to load finalised bets.', 'danger');
    } finally {
      this.isLoading = false;
    }
  }

  getBetStatus(bet: Bet): string {
    if (
      bet.playerHomeGoals === null || bet.playerHomeGoals === undefined ||
      bet.playerAwayGoals === null || bet.playerAwayGoals === undefined
    ) {
      return 'Not Predicted';
    }
  
    if (
      bet.actualHomeGoals === null || bet.actualHomeGoals === undefined ||
      bet.actualAwayGoals === null || bet.actualAwayGoals === undefined
    ) {
      return 'Not Finalized';
    }
  
    if (bet.playerHomeGoals === bet.actualHomeGoals && bet.playerAwayGoals === bet.actualAwayGoals) {
      return 'Exact Match';
    }
  
    let playerBetWinner: string;
    let actualMatchWinner: string;
  
    // Determine Player's Bet Outcome
    if (bet.playerHomeGoals > bet.playerAwayGoals) {
      playerBetWinner = 'home';
    } else if (bet.playerHomeGoals < bet.playerAwayGoals) {
      playerBetWinner = 'away';
    } else {
      playerBetWinner = 'draw';
    }
  
    // Determine Actual Match Outcome
    if (bet.actualHomeGoals !== null && bet.actualAwayGoals !== null) {
      if (bet.actualHomeGoals > bet.actualAwayGoals) {
        actualMatchWinner = 'home';
      } else if (bet.actualHomeGoals < bet.actualAwayGoals) {
        actualMatchWinner = 'away';
      } else {
        actualMatchWinner = 'draw';
      }
    } else {
      return 'Not Finalized';
    }
  
    return playerBetWinner === actualMatchWinner ? 'Won' : 'Lost';
  }

  async openBetsOverview(bet: Bet) {
    console.log("Fetching bet overview data for matchId:", bet.matchId);
  
    try {
      const betStats: BetStats = await firstValueFrom(this.betService.getBetStatsByMatchId(bet.matchId));
      console.log("Received bet overview data:", betStats);
  
      const modal = await this.modalCtrl.create({
        component: BetsOverviewModalComponent,
        componentProps: { betStats }, // Pass the fetched data directly
        breakpoints: [0, 0.75, 1],
        initialBreakpoint: 1,
      });
  
      await modal.present();
  
    } catch (error) {
      console.error("Error fetching bet overview data:", error);
      this.showToast("Failed to load bet overview.", "danger");
    }
  }   

  async showToast(message: string, color: 'success' | 'warning' | 'danger') {
    const toast = await this.toastController.create({
      message,
      duration: 3000,
      position: 'bottom',
      color,
    });
    await toast.present();
  }
}
