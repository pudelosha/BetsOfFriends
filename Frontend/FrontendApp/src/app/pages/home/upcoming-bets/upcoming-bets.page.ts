import { Component, OnInit, Input, Output, EventEmitter, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TournamentSelectionService } from 'src/app/services/tournament-selection.service';
import { UpcomingBet } from 'src/app/model/bet';
import { firstValueFrom } from 'rxjs';
import { ToastController } from '@ionic/angular';
import { BetService } from 'src/app/services/bet.service';
import { Router } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { IonList, IonItem, IonSpinner } from '@ionic/angular/standalone';

@Component({
  selector: 'app-upcoming-bets',
  standalone: true,
  imports: [CommonModule, TranslateModule, IonList, IonItem, IonSpinner],
  templateUrl: './upcoming-bets.page.html',
  styleUrls: ['./upcoming-bets.page.scss']
})
export class UpcomingBetsPage implements OnInit {
  @Input() refreshTrigger: number = 0;
  @Output() loadingStart = new EventEmitter<void>();
  @Output() loadingEnd = new EventEmitter<void>();

  tournamentId: number | null = null;
  upcomingGames: UpcomingBet[] = [];
  isLoading = true;
  errorMessage: string | null = null;
  firstStage: string | null = null;

  constructor(
    private betService: BetService,
    private router: Router,
    private tournamentSelectionService: TournamentSelectionService,
    private toastController: ToastController
  ) {}

  async ngOnInit() {
    //await this.loadTournamentAndFetchBets();
  }

  async ionViewWillEnter() {
    await this.loadTournamentAndFetchBets(); // Refresh messages on page enter
  }

  async ngOnChanges(changes: SimpleChanges) {
    if (changes['refreshTrigger'] && !changes['refreshTrigger'].firstChange) {
      this.loadTournamentAndFetchBets();
    }
  }

  private async loadTournamentAndFetchBets() {
    this.loadingStart.emit();
    this.tournamentId = this.tournamentSelectionService.getSelectedTournament();

    if (this.tournamentId === null) {
      this.isLoading = false;
      this.loadingEnd.emit();
      return;
    }

    await this.loadUpcomingBets();
    this.loadingEnd.emit();
  }

  async loadUpcomingBets() {
    if (this.tournamentId === null) {
      console.error('Tournament ID is null, cannot fetch upcoming bets.');
      return;
    }
  
    try {
      this.upcomingGames = await firstValueFrom(this.betService.getUpcomingBets(this.tournamentId));
  
      // Store the stage from the first item if available
      if (this.upcomingGames.length > 0) {
        this.firstStage = this.upcomingGames[0].stage;
      }
  
    } catch (error) {
      console.error('Error fetching upcoming bets:', error);
      this.errorMessage = 'Failed to load upcoming bets.';
    } finally {
      this.isLoading = false;
    }
  }  

  goToMyBets() {
    const queryParams: any = { tab: 'to-place' };
  
    if (this.firstStage) {
      queryParams.stage = this.firstStage;
    }
  
    this.router.navigate(['/my-bets'], {
      queryParams
    });
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
