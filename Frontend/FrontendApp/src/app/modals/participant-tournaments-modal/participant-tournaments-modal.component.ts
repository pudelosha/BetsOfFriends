import { Component, OnInit, } from '@angular/core';
import { CustomTournamentService } from 'src/app/services/custom-tournament.service';
import { TournamentSelectionService } from 'src/app/services/tournament-selection.service';
import { ModalController, IonicModule } from '@ionic/angular';
import { UserActiveTournament } from 'src/app/model/tournament-model';
import { Router } from '@angular/router';
import { ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ToastController } from '@ionic/angular';
import { NavController } from '@ionic/angular';



@Component({
  selector: 'app-participant-tournaments-modal',
  templateUrl: './participant-tournaments-modal.component.html',
  styleUrls: ['./participant-tournaments-modal.component.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule],
})
export class ParticipantTournamentsModalComponent implements OnInit {
  tournaments: UserActiveTournament[] = [];
  isLoading = true;
  selectedTournamentId: number | null = null;

  constructor(
    private tournamentService: CustomTournamentService,
    private tournamentSelectionService: TournamentSelectionService,
    public modalController: ModalController,
    private router: Router,
    private toastController: ToastController,
    private navCtrl: NavController
  ) {}

  ngOnInit() {
    this.selectedTournamentId = this.tournamentSelectionService.getSelectedTournament();
    this.loadTournaments();
  }
  
  loadTournaments() {
    this.tournamentService.getUserActiveTournaments().subscribe({
      next: (tournaments) => {
        this.tournaments = tournaments;
        this.isLoading = false;
      },
      error: () => {
        this.tournaments = [];
        this.isLoading = false;
      }
    });
  }

  async selectTournament(tournament: UserActiveTournament) {
    this.tournamentSelectionService.setSelectedTournament(tournament.tournamentId);
  
    await this.showToast(`${tournament.tournamentName} selected!`, 'success');
    await this.modalController.dismiss();
    
    const currentUrl = this.router.url;
    await this.router.navigateByUrl('/redirect', { skipLocationChange: true });
    await this.router.navigateByUrl(currentUrl, { skipLocationChange: true });
  }
    
  async showToast(message: string, color: 'success' | 'warning' | 'danger') {
    const toast = await this.toastController.create({
      message,
      duration: 2000,
      position: 'bottom',
      color,
    });
    await toast.present();
  } 
}
