import { Component, Input, Output, EventEmitter, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TournamentInvite } from 'src/app/model/tournament-model';
import { firstValueFrom } from 'rxjs';
import { ToastController } from '@ionic/angular';
import { CustomTournamentService } from 'src/app/services/custom-tournament.service';
import { Router } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { IonList, IonItem } from '@ionic/angular/standalone';

@Component({
  selector: 'app-tournament-invites',
  standalone: true,
  imports: [CommonModule, TranslateModule, IonList, IonItem ],
  templateUrl: './tournament-invites.page.html',
  styleUrls: ['./tournament-invites.page.scss']
})
export class TournamentInvitesPage implements OnChanges {
  @Input() refreshTrigger: number = 0;
  @Output() loadingStart = new EventEmitter<void>();
  @Output() loadingEnd = new EventEmitter<void>();
  
  invites: TournamentInvite[] = [];
  isLoading = true;
  errorMessage: string | null = null;

  constructor(
    private tournamentService: CustomTournamentService,
    private toastController: ToastController,
    private router: Router,
    private translate: TranslateService
  ) {}

  async ionViewWillEnter() {
    await this.loadTournamentInvites();
  }

  async ngOnChanges(changes: SimpleChanges) {
    if (changes['refreshTrigger'] && !changes['refreshTrigger'].firstChange) {
      this.loadTournamentInvites();
    }
  }

  private async loadTournamentInvites() {
    this.loadingStart.emit();

    try {
      this.invites = await firstValueFrom(this.tournamentService.getPendingTournamentInvites());

      if (this.invites.length === 0) {
        this.invites = [];
      }
    } catch (error) {
      console.error('Error fetching tournament invites:', error);
      this.errorMessage = this.translate.instant('TOURNAMENT_INVITES.LOAD_FAILED');
    } finally {
      this.isLoading = false;
      this.loadingEnd.emit();
    }
  }

  goToMyTournaments() {
    this.router.navigate(['/my-tournaments']);
  }

  async showToast(message: string, color: 'success' | 'warning' | 'danger' | 'primary') {
    const toast = await this.toastController.create({
      message,
      duration: 3000,
      position: 'bottom',
      color,
    });
    await toast.present();
  }
}
