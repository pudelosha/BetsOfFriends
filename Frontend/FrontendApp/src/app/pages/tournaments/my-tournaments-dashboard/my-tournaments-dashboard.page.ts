import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonicModule, ToastController, LoadingController, AlertController, ModalController  } from '@ionic/angular';
import { CustomTournamentService } from 'src/app/services/custom-tournament.service';
import { ReactiveFormsModule } from '@angular/forms';
import { UserActiveTournament } from 'src/app/model/tournament-model';
import { firstValueFrom } from 'rxjs';
import { AcceptInvitationModalComponent } from 'src/app/modals/accept-invitation-modal/accept-invitation-modal.component';
import { TournamentSelectionService } from 'src/app/services/tournament-selection.service';
import { Router } from '@angular/router';
import { ChangeDetectorRef } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { TitleService } from 'src/app/services/title.service';


@Component({
  selector: 'app-my-tournaments-dashboard',
  templateUrl: './my-tournaments-dashboard.page.html',
  styleUrls: ['./my-tournaments-dashboard.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule, TranslateModule],
})
export class MyTournamentsDashboardPage implements OnInit {
  tournaments: UserActiveTournament[] = [];
  isLoading = true;
  selectedTournamentId: number | null = null;

  constructor(
    private tournamentService: CustomTournamentService,
    private tournamentSelectionService: TournamentSelectionService,
    private toastController: ToastController,
    private titleService: TitleService,
    private modalController: ModalController,
    private router: Router,
    private cdRef: ChangeDetectorRef,
    private loadingController: LoadingController,
  ) {}

  ngOnInit() {
    this.titleService.setTitle('MY_TOURNAMENTS.TITLE');
    this.selectedTournamentId = this.tournamentSelectionService.getSelectedTournament();
    this.loadTournaments();
  }

  async ionViewWillEnter() {
    this.titleService.setTitle('MY_TOURNAMENTS.TITLE');
    this.selectedTournamentId = this.tournamentSelectionService.getSelectedTournament();
    this.loadTournaments();
  }

  async loadTournaments() {
    this.isLoading = true;
  
    const loading = await this.loadingController.create({
      message: 'Loading tournaments...',
      spinner: 'crescent',
    });
    await loading.present(); // Show spinner
  
    const startTime = Date.now(); // Capture start time
  
    this.tournamentService.getUserActiveTournaments().subscribe({
      next: (response) => {
        this.tournaments = response.map(t => ({
          ...t,
          isVisible: !!t.isVisible // Ensure it's always a boolean
        }));
        //console.log("Loaded tournaments:", this.tournaments); // 🔍 Debug log
      },
      error: (error) => {
        this.showToast('Failed to load tournaments. Please try again later.', 'danger');
        console.error('Error fetching tournaments:', error);
        this.tournaments = [];
      },
      complete: async () => {
        const elapsedTime = Date.now() - startTime;
        const delay = Math.max(0, 500 - elapsedTime); // Ensure at least 500ms delay
  
        setTimeout(async () => {
          this.isLoading = false;
          await loading.dismiss(); // Dismiss spinner after delay
        }, delay);
      }
    });
  }

  async openEditModal(tournament: UserActiveTournament, editMode: boolean): Promise<void> {
    const modal = await this.modalController.create({
      component: AcceptInvitationModalComponent,
      componentProps: {
        tournamentName: tournament.tournamentName,
        tournamentId: tournament.tournamentId,
        editMode
      },
      breakpoints: [0, 0.5, 1],
      initialBreakpoint: 0.5,
    });
  
    await modal.present();
    const { data } = await modal.onWillDismiss();
  
    if (data?.accepted || data?.quit) {
      this.loadTournaments();
    }
  }  
     
  selectTournament(tournament: any): void {
    this.tournamentSelectionService.setSelectedTournament(tournament.tournamentId);
    this.selectedTournamentId = tournament.tournamentId;
    this.router.navigate(['/my-bets']);
  }

  toggleTournamentVisibility(tournament: UserActiveTournament) {
    this.tournamentService.toggleTournamentVisibility(tournament.tournamentId).subscribe({
      next: (updatedVisibility: boolean) => {
        tournament.isVisible = updatedVisibility; // Update the UI immediately
        this.cdRef.detectChanges(); // Force UI refresh
        //console.log(`Tournament ${tournament.tournamentName} visibility: ${tournament.isVisible}`); // 🔍 Debug log
        this.showToast(`Tournament visibility updated!`, 'success');
      },
      error: (error) => {
        console.error('Error toggling visibility:', error);
        this.showToast('Failed to update tournament visibility.', 'danger');
      }
    });
  }
                
  // Show toast messages for notifications
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