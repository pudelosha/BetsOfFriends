import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { ModalController, ToastController, NavController } from '@ionic/angular';
import { IonHeader, IonToolbar, IonTitle, IonButtons, IonButton, IonContent, IonSpinner, IonGrid, IonRow, IonCol } from '@ionic/angular/standalone';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { Router } from '@angular/router';
import { CustomTournamentService } from 'src/app/services/custom-tournament.service';
import { TournamentSelectionService } from 'src/app/services/tournament-selection.service';
import { UserActiveTournament } from 'src/app/model/tournament-model';

@Component({
  selector: 'app-participant-tournaments-modal',
  templateUrl: './participant-tournaments-modal.component.html',
  styleUrls: ['./participant-tournaments-modal.component.scss'],
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TranslateModule, IonHeader, IonToolbar, IonTitle, IonButtons, IonButton, IonContent, IonSpinner, IonGrid, IonRow, IonCol],
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
    private navCtrl: NavController,
    private translate: TranslateService
  ) {}

  ngOnInit() {
    this.selectedTournamentId = this.tournamentSelectionService.getSelectedTournament();
    this.loadTournaments();
  }
  
  loadTournaments() {
    this.tournamentService.getUserActiveTournaments().subscribe({
      next: (tournaments) => {
        this.tournaments = tournaments.filter(t => t.isVisible);
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
  
    await this.showToast(this.t('TOASTS.SELECTED_TOURNAMENT', { name: tournament.tournamentName }), 'success');
    await this.modalController.dismiss();
  
    const cleanUrl = this.router.url.split('?')[0];
    await this.router.navigateByUrl('/redirect', { skipLocationChange: true });
    await this.router.navigateByUrl(cleanUrl);
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

  private t(key: string, params?: Record<string, unknown>): string {
    return this.translate.instant(key, params);
  }
}
