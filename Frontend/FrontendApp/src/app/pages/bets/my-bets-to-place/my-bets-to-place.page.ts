import { Component, OnInit } from '@angular/core';
import { ModalController, ToastController } from '@ionic/angular';
import { EditBetModalComponent } from 'src/app/modals/edit-bet-modal/edit-bet-modal.component';
import { CommonModule } from '@angular/common';
import { IonicModule } from '@ionic/angular';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { BetService } from 'src/app/services/bet.service';
import { TournamentSelectionService } from 'src/app/services/tournament-selection.service';
import { Bet, BetUpdateDto, BetStats } from 'src/app/model/bet';
import { firstValueFrom } from 'rxjs';
import { BetsOverviewModalComponent } from 'src/app/modals/bets-overview-modal/bets-overview-modal.component';


@Component({
  selector: 'app-my-bets-to-place',
  templateUrl: './my-bets-to-place.page.html',
  styleUrls: ['./my-bets-to-place.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule],
})
export class MyBetsToPlacePage implements OnInit {
  showFabButton: boolean = true; // Control FAB visibility
  bets: Bet[] = [];
  isLoading = true;

  constructor(
    private modalCtrl: ModalController,
    private betService: BetService,
    private tournamentSelectionService: TournamentSelectionService,
    private toastController: ToastController
  ) {}

  ngOnInit() {
    console.log('ngOnInit called - Loading bets page...');
    this.loadBets();
  }
  
  ionViewWillEnter() {
    console.log('ionViewWillEnter called - Loading bets page...');
    this.loadBets();
  }
  
  async loadBets() {
    this.isLoading = true;
    const tournamentId = this.tournamentSelectionService.getSelectedTournament();
    console.log("Tournament ID for bets:", tournamentId);
  
    if (!tournamentId) {
      console.warn("No tournament selected.");
      this.isLoading = false;
      return;
    }
  
    try {
      this.bets = await firstValueFrom(this.betService.getBetsByStatus(tournamentId, 'ToPlace'));
      console.log("Bets received:", this.bets);
      if (this.bets.length === 0) {
        console.warn("No bets available.");
      }
    } catch (error) {
      console.error("API error:", error);
    } finally {
      this.isLoading = false;
    }
  }  

  async editBet(bet: Bet, event: Event) {
    event.stopPropagation();
    console.log("Opening Edit Bet Modal:", bet);
  
    const modal = await this.modalCtrl.create({
      component: EditBetModalComponent,
      componentProps: { bet },
      breakpoints: [0, 0.5, 0.75, 1], // Modal sizes
      initialBreakpoint: 1, // Default to 75% height
    });
  
    await modal.present();
  
    const { data } = await modal.onWillDismiss();
    if (data) {
      console.log("Updated Bet Data:", data);
  
      const betUpdate: BetUpdateDto = {
        baseAmount: 1,
        bonusAmount: null, // Always null for now
        homeGoals: data.playerHomeGoals,
        awayGoals: data.playerAwayGoals,
        qualifiedTeam: data.playerQualifiedTeam,
      };
  
      try {
        await firstValueFrom(this.betService.updateBet(bet.betId, betUpdate));
  
        // Remove the bet from the list since it's now "Placed"
        this.bets = this.bets.filter(b => b.betId !== bet.betId);
  
        this.showToast("Bet placed successfully!", "success");
      } catch (error) {
        console.error("Error placing bet:", error);
        this.showToast("Failed to place bet. Please try again.", "danger");
      }
    }
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
