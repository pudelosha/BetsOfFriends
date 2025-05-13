import { Component, OnInit, Input, Output, EventEmitter, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TournamentSelectionService } from 'src/app/services/tournament-selection.service';
import { CustomTournamentService } from 'src/app/services/custom-tournament.service';
import { TournamentPlayerResult } from 'src/app/model/tournament-model';
import { firstValueFrom } from 'rxjs';
import { ToastController } from '@ionic/angular';
import { Router } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { IonList, IonItem } from '@ionic/angular/standalone';

@Component({
  selector: 'app-tournament-results',
  standalone: true,
  imports: [CommonModule, TranslateModule, IonList, IonItem],
  templateUrl: './tournament-results.page.html',
  styleUrls: ['./tournament-results.page.scss']
})
export class TournamentResultsPage implements OnInit {
  @Input() refreshTrigger: number = 0;
  @Output() loadingStart = new EventEmitter<void>();
  @Output() loadingEnd = new EventEmitter<void>();

  tournamentId: number | null = null;
  players: TournamentPlayerResult[] = [];
  isLoading = true;

  constructor(
    private tournamentService: CustomTournamentService,
    private tournamentSelectionService: TournamentSelectionService,
    private router: Router,
    private toastController: ToastController
  ) {}

  async ngOnInit() {
    //await this.loadTournamentAndFetchSummary();
  }

  async ionViewWillEnter() {
    await this.loadTournamentAndFetchSummary(); // Refresh messages on page enter
  }

  async ngOnChanges(changes: SimpleChanges) {
    if (changes['refreshTrigger'] && !changes['refreshTrigger'].firstChange) {
      this.loadTournamentAndFetchSummary();
    }
  }

  private async loadTournamentAndFetchSummary() {
    this.loadingStart.emit();
    this.tournamentId = this.tournamentSelectionService.getSelectedTournament();

    if (this.tournamentId === null) {
      this.isLoading = false;
      this.loadingEnd.emit();
      return;
    }

    await this.loadTournamentSummary();
    this.loadingEnd.emit();
  }

  async loadTournamentSummary() {
    if (this.tournamentId === null) {
      console.error('Tournament ID is null, cannot fetch results.');
      return;
    }

    try {
      this.players = await firstValueFrom(this.tournamentService.getTournamentPlayerResult(this.tournamentId));
    } catch (error) {
      console.error('Error fetching results:', error);
      await this.showToast('Failed to load tournament results', 'danger');
    } finally {
      this.isLoading = false;
    }
  }

  goToMyBets() {
    this.router.navigate(['/results']);
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
