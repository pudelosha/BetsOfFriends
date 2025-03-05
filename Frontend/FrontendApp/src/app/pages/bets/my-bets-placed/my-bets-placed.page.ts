import { Component, ChangeDetectorRef } from '@angular/core';
import { ModalController, ToastController } from '@ionic/angular';
import { EditBetModalComponent } from 'src/app/modals/edit-bet-modal/edit-bet-modal.component';
import { CommonModule } from '@angular/common';
import { IonicModule } from '@ionic/angular';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { BetService } from 'src/app/services/bet.service';
import { TournamentSelectionService } from 'src/app/services/tournament-selection.service';
import { Bet, BetUpdateDto } from 'src/app/model/bet';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-my-bets-placed',
  templateUrl: './my-bets-placed.page.html',
  styleUrls: ['./my-bets-placed.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule, FormsModule],
})
export class MyBetsPlacedPage {
  bets: Bet[] = [];
  isLoading = true;

  constructor(
    private modalCtrl: ModalController,
    private betService: BetService,
    private tournamentSelectionService: TournamentSelectionService,
    private toastController: ToastController,
    private cdRef: ChangeDetectorRef // Added ChangeDetectorRef
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

    if (!tournamentId) {
      this.showToast('No tournament selected!', 'warning');
      this.isLoading = false;
      return;
    }

    try {
      this.bets = await firstValueFrom(this.betService.getBetsByStatus(tournamentId, 'Placed'));
      console.log('Loaded Placed Bets:', this.bets);
    } catch (error) {
      console.error('Error fetching placed bets:', error);
      this.showToast('Failed to load placed bets.', 'danger');
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

      // Construct `BetUpdateDto` for backend
      const betUpdate: BetUpdateDto = {
        baseAmount: 1, // Always 1
        bonusAmount: null, // Always null for now
        homeGoals: data.homeGoals,
        awayGoals: data.awayGoals,
        qualifiedTeam: data.qualifies
      };

      try {
        await firstValueFrom(this.betService.updateBet(bet.betId, betUpdate));

        // Update the bet inside the array instead of re-fetching everything
        const index = this.bets.findIndex(b => b.betId === bet.betId);
        if (index !== -1) {
          // Update bet properties
          this.bets[index] = { ...this.bets[index], ...betUpdate };
          
          // Create a new reference to trigger change detection
          this.bets = [...this.bets];

          // Manually detect changes
          this.cdRef.detectChanges();
        }

        this.showToast("Bet updated successfully!", "success");
      } catch (error) {
        console.error("Error updating bet:", error);
        this.showToast("Failed to update bet. Please try again.", "danger");
      }
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
