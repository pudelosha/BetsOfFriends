import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToastController, AlertController, LoadingController } from '@ionic/angular';
import { PredefinedTournamentService } from 'src/app/services/predefined-tournament.service';
import { Tournament } from 'src/app/model/tournament-model';
import { firstValueFrom } from 'rxjs';
import { Router } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { TitleService } from 'src/app/services/title.service';
import { IonContent, IonList, IonItem, IonGrid, IonRow, IonCol, IonButton, IonIcon, IonSpinner } from '@ionic/angular/standalone';

@Component({
  selector: 'app-predefined-tournaments-list',
  templateUrl: './predefined-tournaments-list.page.html',
  styleUrls: ['./predefined-tournaments-list.page.scss'],
  standalone: true,
  imports: [CommonModule, TranslateModule, IonContent, IonList, IonItem, IonGrid, IonRow, IonCol, IonButton, IonIcon, IonSpinner],
})
export class PredefinedTournamentsListPage implements OnInit {
  tournaments: Tournament[] = [];
  isLoading = true;

  constructor(
    private tournamentService: PredefinedTournamentService,
    private toastController: ToastController,
    private alertController: AlertController,
    private router: Router,
    private loadingController: LoadingController,
    private titleService: TitleService,
    private translate: TranslateService
  ) {}

  ngOnInit() {
    this.titleService.setTitle('PREDEFINED_TOURNAMENTS.TITLE');
    this.loadTournaments();
  }

  ionViewWillEnter() {
    this.titleService.setTitle('PREDEFINED_TOURNAMENTS.TITLE');
    this.loadTournaments();
    this.scrollToTop();
  }

  private scrollToTop(): void {
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  async loadTournaments() {
    this.isLoading = true;

    const loading = await this.loadingController.create({
      message: this.t('TOASTS.LOADING_TOURNAMENTS'),
      spinner: 'crescent',
    });
    await loading.present();

    const startTime = Date.now();

    try {
      this.tournaments = await firstValueFrom(this.tournamentService.getPredefinedTournaments());
    } catch (err) {
      console.error('Error loading tournaments:', err);
      this.tournaments = [];
    } finally {
      const elapsedTime = Date.now() - startTime;
      const delay = Math.max(0, 500 - elapsedTime);

      setTimeout(async () => {
        this.isLoading = false;
        await loading.dismiss();
      }, delay);
    }
  }
  
  editTournament(tournament: Tournament): void {
    if (!tournament || !tournament.tournamentId) {
      console.error('Invalid tournament object:', tournament);
      this.showToast(this.t('TOASTS.TOURNAMENT_DATA_INVALID'), 'danger');
      return;
    }

    this.router.navigate([`/tournaments/update-predefined/${tournament.tournamentId}`]).catch((error) => {
      console.error('Navigation to edit tournament failed:', error);
      this.showToast(this.t('TOASTS.TOURNAMENT_EDITOR_NAV_FAILED'), 'danger');
    });
  }

  editMatches(tournamentId: number | null | undefined): void {
    if (tournamentId == null) {
      console.warn('Invalid tournament ID');
      return;
    }
  
    this.router.navigate(['/matches/predefined', tournamentId]);
  }  
  
  async toggleTournamentStatus(tournament: any) {
    const newStatus = !tournament.isActive;

    const loading = await this.loadingController.create({
      message: this.t('TOASTS.UPDATING_TOURNAMENT'),
      spinner: 'crescent',
    });
    await loading.present();

    const startTime = Date.now();

    try {
      await this.tournamentService.updatePredefinedTournamentStatus(tournament.tournamentId, newStatus).toPromise();
      tournament.isActive = newStatus;
      this.showToast(this.t(newStatus ? 'TOASTS.TOURNAMENT_ENABLED' : 'TOASTS.TOURNAMENT_DISABLED'), 'success');
    } catch (error) {
      tournament.isActive = !newStatus;
      console.error('Error toggling tournament status:', error);
      this.showToast(this.t('TOASTS.TOURNAMENT_TOGGLE_FAILED'), 'danger');
    } finally {
      const elapsedTime = Date.now() - startTime;
      const delay = Math.max(0, 500 - elapsedTime);

      setTimeout(async () => {
        await loading.dismiss();
      }, delay);
    }
  }

  async deleteTournament(tournament: any) {
    const alert = await this.alertController.create({
        header: this.t('TOASTS.CONFIRM_DELETE_TITLE'),
        message: this.t('TOASTS.CONFIRM_DELETE_TOURNAMENT', { name: tournament.tournamentName }),
        buttons: [
            { text: this.t('TOASTS.CANCEL'), role: 'cancel' },
            {
                text: this.t('TOASTS.DELETE'),
                handler: async () => {
                    const loading = await this.loadingController.create({
                        message: this.t('TOASTS.DELETING_TOURNAMENT'),
                        spinner: 'crescent',
                    });
                    await loading.present();

                    const startTime = Date.now();

                    try {
                        await firstValueFrom(this.tournamentService.deletePredefinedTournament(tournament.tournamentId));
                        this.tournaments = this.tournaments.filter(t => t.tournamentId !== tournament.tournamentId);

                        this.showToast(this.t('TOASTS.TOURNAMENT_DELETED'), 'success');
                    } catch (error) {
                        this.showToast(this.t('TOASTS.TOURNAMENT_DELETE_FAILED'), 'danger');
                        console.error(error);
                    } finally {
                        const elapsedTime = Date.now() - startTime;
                        const delay = Math.max(0, 500 - elapsedTime);

                        setTimeout(async () => {
                            await loading.dismiss();
                        }, delay);
                    }
                }
            }
        ]
    });

    await alert.present();
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

  private t(key: string, params?: Record<string, unknown>): string {
    return this.translate.instant(key, params);
  }
}
