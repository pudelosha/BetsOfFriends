import { Component, OnInit } from '@angular/core';
import { ModalController, ToastController } from '@ionic/angular';
import { EditMatchResultModalComponent } from 'src/app/modals/edit-match-result-modal/edit-match-result-modal.component';
import { CommonModule } from '@angular/common';
import { IonicModule } from '@ionic/angular';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { TournamentSelectionService } from 'src/app/services/tournament-selection.service';
import { firstValueFrom } from 'rxjs';
import { MatchService } from 'src/app/services/match.service';
import { Match } from 'src/app/model/match';

@Component({
  selector: 'app-manage-upcoming',
  templateUrl: './manage-upcoming.page.html',
  styleUrls: ['./manage-upcoming.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule, FormsModule],
})
export class ManageUpcomingPage implements OnInit {
  matches: Match[] = [];
  isLoading = true;

  constructor(
    private modalCtrl: ModalController,
    private matchService: MatchService,
    private tournamentSelectionService: TournamentSelectionService,
    private toastController: ToastController
  ) {}

  ngOnInit() {
    console.log('Loading upcoming matches page...');
    this.loadMatches(); 
  }

  ionViewWillEnter() {
    console.log('Reloading upcoming matches...');
    this.loadMatches(); 
  }

  async loadMatches() {
    this.isLoading = true;
    const tournamentId = this.tournamentSelectionService.getSelectedTournament();
    console.log("Selected Tournament ID:", tournamentId);

    if (!tournamentId) {
      console.warn("No tournament selected.");
      this.isLoading = false;
      return;
    }

    try {
      this.matches = await firstValueFrom(this.matchService.getMatchesByStatus(tournamentId, 'Upcoming'));
      console.log("Matches received:", this.matches);
    } catch (error) {
      console.error("API error:", error);
    } finally {
      this.isLoading = false;
    }
  }

  async editMatchResult(match: Match, event: Event) {
    event.stopPropagation();
    console.log("Opening Edit Match Result Modal:", match);
  
    const modal = await this.modalCtrl.create({
      component: EditMatchResultModalComponent,
      componentProps: { match },
      breakpoints: [0, 0.5, 0.75, 1],
      initialBreakpoint: 1,
    });
  
    await modal.present();
  
    const { data } = await modal.onWillDismiss();
    if (data) {
      console.log("Updated Match Result Data:", data);
  
      try {
        await firstValueFrom(this.matchService.updateMatchResult(match.matchId, data));
  
        // Refresh the entire match list instead of updating only one item
        await this.loadMatches();  
  
        this.showToast("Match result updated successfully!", "success");
      } catch (error) {
        console.error("Error updating match result:", error);
        this.showToast("Failed to update match result. Please try again.", "danger");
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
