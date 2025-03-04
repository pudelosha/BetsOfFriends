import { Component, OnInit } from '@angular/core';
import { ModalController, ToastController } from '@ionic/angular';
import { EditBetModalComponent } from 'src/app/modals/edit-bet-modal/edit-bet-modal.component';
import { CommonModule } from '@angular/common';
import { IonicModule } from '@ionic/angular';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { BetService } from 'src/app/services/bet.service';
import { TournamentSelectionService } from 'src/app/services/tournament-selection.service';
import { Bet } from 'src/app/model/bet';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-my-bets-to-place',
  templateUrl: './my-bets-to-place.page.html',
  styleUrls: ['./my-bets-to-place.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule, FormsModule],
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

  async ngOnInit() {
    console.log('loading bets page');
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
      this.bets = await firstValueFrom(this.betService.getBetsByStatus(tournamentId, 'ToPlace'));
      console.log('Loaded Bets:', this.bets);
    } catch (error) {
      console.error('Error fetching bets:', error);
      this.showToast('Failed to load bets.', 'danger');
    } finally {
      this.isLoading = false;
    }
  }

  async editBet(bet: Bet, event: Event) {
    event.stopPropagation();
    console.log("Bet Clicked:", bet);

    const modal = await this.modalCtrl.create({
      component: EditBetModalComponent,
      componentProps: { bet }
    });

    return await modal.present();
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
