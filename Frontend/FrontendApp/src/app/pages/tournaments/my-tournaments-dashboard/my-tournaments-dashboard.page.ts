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

@Component({
  selector: 'app-my-tournaments-dashboard',
  templateUrl: './my-tournaments-dashboard.page.html',
  styleUrls: ['./my-tournaments-dashboard.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule],
})
export class MyTournamentsDashboardPage implements OnInit {
  tournaments: UserActiveTournament[] = [];
  isLoading = true;

  constructor(
    private tournamentService: CustomTournamentService,
    private tournamentSelectionService: TournamentSelectionService,
    private toastController: ToastController,
    private alertController: AlertController,
    private modalController: ModalController,
    private router: Router,
    private cdRef: ChangeDetectorRef,
  ) {}

  ngOnInit() {
    this.loadTournaments();
  }

  async ionViewWillEnter() {
    this.loadTournaments();
  }

  loadTournaments() {
    this.isLoading = true;
    this.tournamentService.getUserActiveTournaments().subscribe({
      next: (response) => {
        this.tournaments = response.map(t => ({
          ...t,
          isVisible: !!t.isVisible // Ensure it's always a boolean
        }));
        console.log("Loaded tournaments:", this.tournaments); // 🔍 Debug log
      },
      error: (error) => {
        this.showToast('Failed to load tournaments. Please try again later.', 'danger');
        console.error('Error fetching tournaments:', error);
        this.tournaments = [];
      },
      complete: () => {
        this.isLoading = false;
      }
    });
  }
   
  selectTournament(tournament: any): void {
    this.tournamentSelectionService.setSelectedTournament(tournament.tournamentId);
    console.log(this.tournamentSelectionService.getSelectedTournament());
    this.router.navigate(['/my-bets']);
  }

  toggleTournamentVisibility(tournament: UserActiveTournament) {
    this.tournamentService.toggleTournamentVisibility(tournament.tournamentId).subscribe({
      next: (updatedVisibility: boolean) => {
        tournament.isVisible = updatedVisibility; // Update the UI immediately
        this.cdRef.detectChanges(); // Force UI refresh
        console.log(`Tournament ${tournament.tournamentName} visibility: ${tournament.isVisible}`); // 🔍 Debug log
        this.showToast(`Tournament visibility updated!`, 'success');
      },
      error: (error) => {
        console.error('Error toggling visibility:', error);
        this.showToast('Failed to update tournament visibility.', 'danger');
      }
    });
  }
          
  async acceptInvitation(tournament: any): Promise<void> {
    const modal = await this.modalController.create({
      component: AcceptInvitationModalComponent,
      componentProps: {
        tournamentName: tournament.tournamentName,
        tournamentId: tournament.tournamentId,
      },
      breakpoints: [0, 0.5, 1],
      initialBreakpoint: 0.5,
    });
  
    await modal.present();
  
    const { data } = await modal.onWillDismiss();
  
    if (data?.accepted) {
      this.loadTournaments();
    }
  }
    
  async quitTournament(tournament: UserActiveTournament) {
    const alert = await this.alertController.create({
      header: 'Confirm Quit',
      message: `Are you sure you want to quit ${tournament.tournamentName}?`,
      buttons: [
        { text: 'Cancel', role: 'cancel' },
        {
          text: 'Quit',
          handler: async () => {
            try {
              await firstValueFrom(this.tournamentService.quitTournament(tournament.tournamentId));
              this.showToast(`You have quit ${tournament.tournamentName} successfully.`, 'success');
              this.loadTournaments(); // Refresh list after quitting
            } catch (error) {
              console.error('Error quitting tournament:', error);
              this.showToast('Failed to quit tournament. Please try again.', 'danger');
            }
          },
        },
      ],
    });
    await alert.present();
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