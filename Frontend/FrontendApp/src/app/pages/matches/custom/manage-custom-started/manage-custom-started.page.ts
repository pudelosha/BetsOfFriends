import { Component, Input, OnInit, OnChanges, SimpleChanges  } from '@angular/core';
import { ModalController, ToastController, LoadingController } from '@ionic/angular';
import { EditMatchResultModalComponent } from 'src/app/modals/edit-match-result-modal/edit-match-result-modal.component';
import { CommonModule } from '@angular/common';
import { IonicModule } from '@ionic/angular';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { TournamentSelectionService } from 'src/app/services/tournament-selection.service';
import { firstValueFrom } from 'rxjs';
import { CustomMatchService } from 'src/app/services/custom-match.service';
import { Match } from 'src/app/model/match';
import { HttpErrorResponse } from '@angular/common/http';
import { TranslateModule } from '@ngx-translate/core';


@Component({
  selector: 'app-manage-custom-started',
  templateUrl: './manage-custom-started.page.html',
  styleUrls: ['./manage-custom-started.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule, FormsModule, TranslateModule],
})
export class ManageCustomStartedPage implements OnInit, OnChanges {
  @Input() stage!: string; // Receive stage from parent
  
  matches: Match[] = [];
  isLoading = true;
  errorMessage: string = '';

  constructor(
    private modalCtrl: ModalController,
    private matchService: CustomMatchService,
    private tournamentSelectionService: TournamentSelectionService,
    private toastController: ToastController,
    private loadingController: LoadingController
  ) {}

  ngOnInit() {
    console.log('Loading started matches page...');
    this.loadMatches(); 
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['stage'] && !changes['stage'].firstChange) {
      console.log(`Stage changed to: ${this.stage}, reloading matches.`);
      this.loadMatches();
    }
  }  
  
  ionViewWillEnter() {
    console.log('Reloading started matches...');
    this.loadMatches(); 
  }
  
  async loadMatches() {
    this.isLoading = true;
    this.matches = [];
    this.errorMessage = '';
  
    const loading = await this.loadingController.create({
      message: 'Loading matches...',
      spinner: 'crescent',
    });
    await loading.present();
  
    const startTime = Date.now(); // Start timer
  
    const tournamentId = this.tournamentSelectionService.getSelectedTournament();
  
    if (!tournamentId) {
      console.warn("No tournament selected.");
      this.isLoading = false;
      this.errorMessage = "No tournament selected.";
      await loading.dismiss();
      return;
    }
  
    try {
      this.matches = await firstValueFrom(
        this.matchService.getMatchesByTournamentStage(tournamentId, 'InProgress', this.stage)
      );
  
      if (!this.matches.length) {
        this.errorMessage = "No matches available for this stage.";
      }
    } catch (error: unknown) {
      console.error("API error:", error);
  
      if (error instanceof HttpErrorResponse) {
        this.errorMessage = error.status === 404 ? "No matches found for the given criteria" : `An error occurred: ${error.message}`;
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