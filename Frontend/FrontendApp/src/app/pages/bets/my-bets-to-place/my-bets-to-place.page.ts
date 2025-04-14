import { Component, Input, OnInit, SimpleChanges  } from '@angular/core';
import { ModalController, ToastController, LoadingController } from '@ionic/angular';
import { EditBetModalComponent } from 'src/app/modals/edit-bet-modal/edit-bet-modal.component';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { BetService } from 'src/app/services/bet.service';
import { TournamentSelectionService } from 'src/app/services/tournament-selection.service';
import { Bet, BetUpdateDto, BetStats } from 'src/app/model/bet';
import { firstValueFrom } from 'rxjs';
import { BetsOverviewModalComponent } from 'src/app/modals/bets-overview-modal/bets-overview-modal.component';
import { HttpErrorResponse } from '@angular/common/http';
import { TranslateModule } from '@ngx-translate/core';
import { IonSpinner, IonList, IonItem, IonButton, IonIcon } from '@ionic/angular/standalone';

@Component({
  selector: 'app-my-bets-to-place',
  templateUrl: './my-bets-to-place.page.html',
  styleUrls: ['./my-bets-to-place.page.scss'],
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TranslateModule, IonSpinner, IonList, IonItem, IonButton, IonIcon],
})
export class MyBetsToPlacePage implements OnInit {
  @Input() stage!: string; // Receive stage from parent

  showFabButton: boolean = true; // Control FAB visibility
  bets: Bet[] = [];
  isLoading = true;
  errorMessage: string = '';

  constructor(
    private modalCtrl: ModalController,
    private betService: BetService,
    private tournamentSelectionService: TournamentSelectionService,
    private toastController: ToastController,
    private loadingController: LoadingController
  ) {}

  ngOnInit() {
    //console.log('ngOnInit - Child Page');
    this.loadBets();
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['stage'] && !changes['stage'].firstChange) {
      //console.log(`Stage changed to: ${this.stage}, reloading matches.`);
      this.loadBets();
    }
  }   
  
  ionViewDidEnter() {
    //console.log('ionViewDidEnter - Child Page, refreshing bets...');
    this.loadBets();
  }
  
  async loadBets() {
    if (!this.stage) {
      return;
    }

    this.isLoading = true;
    this.bets = [];
    this.errorMessage = '';
  
    const loading = await this.loadingController.create({
      message: 'Loading bets...',
      spinner: 'crescent',
    });
    await loading.present();
  
    const startTime = Date.now();
  
    const tournamentId = this.tournamentSelectionService.getSelectedTournament();
  
    if (!tournamentId) {
      console.warn("No tournament selected.");
      this.isLoading = false;
      this.errorMessage = "No tournament selected.";
      await loading.dismiss();
      return;
    }
  
    try {
      this.bets = await firstValueFrom(
        this.betService.getBetsByTournamentStage(tournamentId, 'ToPlace', this.stage)
      );
  
      if (!this.bets.length) {
        this.errorMessage = "No bets available for this stage.";
      }
    } catch (error: unknown) {
      console.error("API error:", error);
  
      if (error instanceof HttpErrorResponse) {
        this.errorMessage = `An error occurred: ${error.message}`;
      } else {
        this.errorMessage = "An unexpected error occurred";
      }
    } finally {
      const elapsedTime = Date.now() - startTime;
      const delay = Math.max(0, 200 - elapsedTime);
  
      setTimeout(async () => {
        this.isLoading = false;
        await loading.dismiss();
      }, delay);
    }
  }
    
  async editBet(bet: Bet, event: Event) {
    event.stopPropagation();
    //console.log("Opening Edit Bet Modal:", bet);
  
    const modal = await this.modalCtrl.create({
      component: EditBetModalComponent,
      componentProps: { bet },
      breakpoints: [0, 0.5, 0.75, 1], // Modal sizes
      initialBreakpoint: 1, // Default to 75% height
    });
  
    await modal.present();
  
    const { data } = await modal.onWillDismiss();
    if (data) {
      //console.log("Updated Bet Data:", data);
  
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
    //console.log("Fetching bet overview data for matchId:", bet.matchId);
  
    try {
      const betStats: BetStats = await firstValueFrom(this.betService.getBetStatsByMatchId(bet.matchId));
      //console.log("Received bet overview data:", betStats);
  
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
