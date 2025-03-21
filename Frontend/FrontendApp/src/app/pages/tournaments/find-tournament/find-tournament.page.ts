import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonicModule, ToastController, LoadingController, AlertController, ModalController  } from '@ionic/angular';
import { CustomTournamentService } from 'src/app/services/custom-tournament.service';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { PublicTournament } from 'src/app/model/tournament-model';
import { JoinRequestModalComponent } from 'src/app/modals/join-request-modal/join-request-modal.component';

@Component({
  selector: 'app-find-tournament',
  templateUrl: './find-tournament.page.html',
  styleUrls: ['./find-tournament.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule, FormsModule],
})
export class FindTournamentPage implements OnInit {
  searchTerm = '';
  isLoading = false;
  filteredTournaments: PublicTournament[] = [];

  constructor(private tournamentService: CustomTournamentService,
              private modalController: ModalController,
              private toastController: ToastController
  ) {}

  ngOnInit() {
    this.loadTournaments(); // Initial load with all public tournaments
  }

  onSearchChange() {
    this.loadTournaments(); // Filter when input changes
  }

  loadTournaments() {
    this.isLoading = true;

    const trimmedTerm = this.searchTerm.trim();

    // Always pass a trimmed term (empty string means "no search")
    this.tournamentService.searchPublicTournaments(trimmedTerm).subscribe({
      next: (result) => {
        this.filteredTournaments = result;
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Failed to load public tournaments', err);
        this.isLoading = false;
      }
    });
  }

  async requestToJoin(tournament: PublicTournament) {
    const modal = await this.modalController.create({
      component: JoinRequestModalComponent,
      componentProps: {
        tournamentId: tournament.tournamentId,
        tournamentName: tournament.tournamentName
      }
    });
  
    await modal.present();
  
    const { data } = await modal.onDidDismiss();
  
    if (data?.requested) {
      await this.showToast('Join request submitted successfully.', 'success');
      this.loadTournaments(); // Refresh the list to reflect the updated status
    }
  }

  async showToast(message: string, color: 'success' | 'danger' | 'warning') {
    const toast = await this.toastController.create({
      message,
      duration: 3000,
      position: 'bottom',
      color
    });
    await toast.present();
  }
}
