import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ModalController } from '@ionic/angular';
import { IonHeader, IonToolbar, IonTitle, IonButtons, IonButton, IonIcon, IonContent, IonSpinner, IonItem, IonLabel, IonSelect, IonSelectOption, IonAccordionGroup, IonAccordion, IonGrid, IonRow, IonCol } from '@ionic/angular/standalone';
import { TranslateModule } from '@ngx-translate/core';
import { CustomTournamentService } from 'src/app/services/custom-tournament.service';
import { UserBettingStats } from 'src/app/model/tournament-model';

@Component({
  selector: 'app-player-stats-modal',
  templateUrl: './player-stats-modal.component.html',
  styleUrls: ['./player-stats-modal.component.scss'],
  standalone: true,
  imports: [CommonModule, FormsModule, TranslateModule, IonHeader, IonToolbar, IonTitle, IonIcon, IonButtons, IonButton, IonContent, IonSpinner, IonItem, IonLabel, IonSelect, IonSelectOption, IonAccordionGroup, IonAccordion, IonGrid, IonRow, IonCol],
})
export class PlayerStatsModalComponent implements OnInit {
  @Input() tournamentId!: number;
  @Input() userId!: string;

  isLoading = true;
  stats: UserBettingStats[] = [];
  expandedMatchId: number | null = null;
  selectedStage: string | null = null;
  availableStages: string[] = [];

  constructor(
    private modalController: ModalController,
    private tournamentService: CustomTournamentService
  ) {}

  ngOnInit() {
    this.fetchUserStats();
  }

  onAccordionChange(event: Event) {
    const customEvent = event as CustomEvent;
    const expandedMatchId = customEvent.detail?.value;  
    this.expandedMatchId = expandedMatchId !== undefined ? Number(expandedMatchId) : null;
  }
      
  fetchUserStats() {
    this.tournamentService.getUserBettingStats(this.tournamentId, this.userId).subscribe({
      next: (data) => {
        this.stats = data;
  
        const uniqueStages = [...new Set(data.map(s => s.stage))];
        this.availableStages = uniqueStages;
        this.selectedStage = uniqueStages[0] ?? null;
  
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error fetching player stats:', error);
        this.isLoading = false;
      }
    });
  }
  
  get filteredStats(): UserBettingStats[] {
    return this.selectedStage
      ? this.stats.filter(s => s.stage === this.selectedStage)
      : this.stats;
  }
  
  closeModal() {
    this.modalController.dismiss();
  }

  getStatusIcon(value?: string): string {
    return value === 'V' ? '✔' : value === 'X' ? '✘' : '-';
  }
  
  getStatusClass(value?: string): string {
    return value === 'V' ? 'v-status' : value === 'X' ? 'x-status' : '';
  }  

  blurBeforeOpen() {
    requestAnimationFrame(() => {
      const active = document.activeElement as HTMLElement | null;
      if (active) active.blur();
    });
  }
  
  get showQualified(): boolean {
    return this.stats.some(s => s.showQualified);
  }
  
  get showExact(): boolean {
    return this.stats.some(s => s.showExactResult);
  }
    
  get columnGridTemplate(): string {
    const base = ['1fr', '1fr', '1fr'];
    if (this.showExact) base.push('1fr', '1fr');
    if (this.showQualified) base.push('1fr', '1fr', '1fr');
    return base.join(' ');
  }  

  get selectedStageIndex(): number {
    return this.availableStages.findIndex(stage => stage === this.selectedStage);
  }
  
  prevStage(): void {
    const index = this.selectedStageIndex;
    if (index > 0) {
      this.selectedStage = this.availableStages[index - 1];
    }
  }
  
  nextStage(): void {
    const index = this.selectedStageIndex;
    if (index < this.availableStages.length - 1) {
      this.selectedStage = this.availableStages[index + 1];
    }
  }
}
