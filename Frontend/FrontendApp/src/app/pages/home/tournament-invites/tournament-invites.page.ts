import { Component, OnInit, Input, Output, EventEmitter, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TournamentInvite } from 'src/app/model/tournament-model';
import { firstValueFrom } from 'rxjs';
import { IonicModule, ToastController } from '@ionic/angular';
import { CustomTournamentService } from 'src/app/services/custom-tournament.service';
import { Router } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';


@Component({
  selector: 'app-tournament-invites',
  standalone: true,
  imports: [CommonModule, IonicModule, TranslateModule],
  templateUrl: './tournament-invites.page.html',
  styleUrls: ['./tournament-invites.page.scss']
})
export class TournamentInvitesPage implements OnInit {
  @Input() refreshTrigger: number = 0;
  @Output() loadingStart = new EventEmitter<void>();
  @Output() loadingEnd = new EventEmitter<void>();
  
  invites: TournamentInvite[] = [];
  isLoading = true;
  errorMessage: string | null = null;

  constructor(
    private tournamentService: CustomTournamentService,
    private toastController: ToastController,
    private router: Router
  ) {}

  async ngOnInit() {
    await this.loadTournamentInvites();
  }

  async ionViewWillEnter() {
    await this.loadTournamentInvites(); // Refresh messages on page enter
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

      // Hide component if no invites
      if (this.invites.length === 0) {
        this.invites = [];
      }
    } catch (error) {
      console.error('Error fetching tournament invites:', error);
      this.errorMessage = 'Failed to load tournament invites.';
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
