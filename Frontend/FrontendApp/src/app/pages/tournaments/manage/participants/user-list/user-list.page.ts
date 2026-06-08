import { Component, OnInit, Input, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToastController, LoadingController, AlertController } from '@ionic/angular';
import { firstValueFrom } from 'rxjs';
import { TournamentSelectionService } from 'src/app/services/tournament-selection.service';
import { CustomTournamentService } from 'src/app/services/custom-tournament.service';
import { TournamentParticipant } from 'src/app/model/tournament-model';
import { Router } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { IonList, IonItem, IonButton, IonSpinner } from '@ionic/angular/standalone';
import { BackendMessageService } from 'src/app/services/backend-message.service';

@Component({
  selector: 'app-user-list',
  templateUrl: './user-list.page.html',
  styleUrls: ['./user-list.page.scss'],
  standalone: true,
  imports: [CommonModule, TranslateModule, IonList, IonItem, IonButton, IonSpinner],
})
export class UserListPage implements OnInit {
  @Input() refreshTrigger: number = 0;

  tournamentId: number | null = null;
  participants: TournamentParticipant[] = [];
  isLoading = true;

  constructor(
    private tournamentService: CustomTournamentService,
    private tournamentSelectionService: TournamentSelectionService,
    private toastController: ToastController,
    private loadingController: LoadingController,
    private router: Router,
    private alertController: AlertController,
    private translate: TranslateService,
    private backendMessages: BackendMessageService
  ) {}

  async ngOnInit() {
    await this.loadTournamentAndFetchParticipants();
  }

  async ngOnChanges(changes: SimpleChanges) {
    if (changes['refreshTrigger'] && !changes['refreshTrigger'].firstChange) {
      this.loadTournamentAndFetchParticipants();
    }
  }

  private async loadTournamentAndFetchParticipants() {
    this.tournamentId = this.tournamentSelectionService.getSelectedTournament();

    if (this.tournamentId === null) {
      this.isLoading = false;
      return;
    }

    await this.loadParticipants();
  }

  async loadParticipants() {
    const loading = await this.loadingController.create({
      message: this.t('USERS_LIST.LOADING_PARTICIPANTS'),
      spinner: 'crescent'
    });
    await loading.present();

    try {
      this.participants = await firstValueFrom(
        this.tournamentService.getTournamentParticipants(this.tournamentId!)
      );
    } catch (error) {
      console.error('Error loading participants:', error);
      await this.showToast(this.t('USERS_LIST.LOAD_FAILED'), 'danger');
    } finally {
      this.isLoading = false;
      await loading.dismiss();
    }
  }

  async confirmExclude(email: string) {
    const alert = await this.alertController.create({
      header: this.t('USERS_LIST.EXCLUDE_TITLE'),
      message: this.t('USERS_LIST.EXCLUDE_CONFIRMATION', { email }),
      buttons: [
        {
          text: this.t('USERS_LIST.CANCEL'),
          role: 'cancel'
        },
        {
          text: this.t('USERS_LIST.EXCLUDE'),
          role: 'destructive',
          handler: () => {
            this.excludeParticipant(email);
          }
        }
      ]
    });
    await alert.present();
  }

  async excludeParticipant(userEmail: string) {  
    this.tournamentId = this.tournamentSelectionService.getSelectedTournament();
  
    this.tournamentService.excludeParticipant(this.tournamentId!, userEmail).subscribe({
      next: async (result) => {
        if (result.success) {
          await this.showToast(this.t('USERS_LIST.EXCLUDED', { email: userEmail }), 'success');
          await this.loadParticipants();
        } else {
          await this.showToast(
            this.backendMessages.translateMessage(result.message, 'USERS_LIST.EXCLUDE_FAILED'),
            'danger'
          );
        }
      },
      error: async (err) => {
        console.error('Error excluding participant:', err);
        await this.showToast(
          this.backendMessages.translateMessage(this.extractBackendMessage(err), 'USERS_LIST.EXCLUDE_FAILED'),
          'danger'
        );
      }
    });
  }  
  
  async showToast(message: string, color: 'success' | 'warning' | 'danger' | 'primary') {
    const toast = await this.toastController.create({
      message,
      duration: 3000,
      position: 'bottom',
      color
    });
    await toast.present();
  }

  private t(key: string, params?: Record<string, unknown>): string {
    return this.translate.instant(key, params);
  }

  private extractBackendMessage(error: unknown): string | undefined {
    const maybeHttpError = error as { error?: { message?: string; Message?: string } };
    return maybeHttpError?.error?.message || maybeHttpError?.error?.Message;
  }
}
