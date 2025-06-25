import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TournamentSelectionService } from 'src/app/services/tournament-selection.service';
import { CustomTournamentService } from 'src/app/services/custom-tournament.service';
import { firstValueFrom } from 'rxjs';
import { IonContent, IonSpinner, IonGrid, IonRow, IonCol, IonButton, IonIcon, IonSelect, IonSelectOption, IonAccordionGroup, IonAccordion, IonItem, IonLabel } from '@ionic/angular/standalone';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { TitleService } from 'src/app/services/title.service';
import { MatchInsight } from 'src/app/model/match';

@Component({
  selector: 'app-match-insights',
  templateUrl: './match-insights.page.html',
  styleUrls: ['./match-insights.page.scss'],
  standalone: true,
  imports: [CommonModule, FormsModule, TranslateModule, IonContent, IonSpinner, IonGrid, IonRow, IonCol, IonButton, IonIcon, IonSelect, IonSelectOption, IonAccordionGroup, IonAccordion, IonItem, IonLabel]
})
export class MatchInsightsPage {
  isLoading = true;
  insights: MatchInsight[] = [];
  expandedMatchId: number | null = null;
  selectedStage: string | null = null;
  availableStages: string[] = [];
  selectedStageIndex = 0;

  constructor(private translate: TranslateService,
              private tournamentService: CustomTournamentService,
              private titleService: TitleService,           
              private tournamentSelectionService: TournamentSelectionService) {}

  ionViewWillEnter() {
    this.titleService.setTitle('INSIGHTS.TITLE');
    this.loadData();
  }

  async loadData() {
    const tournamentId = this.tournamentSelectionService.getSelectedTournament();

    if (!tournamentId) {
      console.warn('No tournament selected.');
      this.isLoading = false;
      return;
    }

    try {
      this.insights = await firstValueFrom(this.tournamentService.getMatchInsights(tournamentId));
      this.availableStages = [...new Set(this.insights.map(i => i.stage))];
      this.selectedStage = this.availableStages[0] ?? null;
      this.selectedStageIndex = 0;
    } catch (error) {
      console.error('Error fetching match insights:', error);
    } finally {
      this.isLoading = false;
    }
  }

  get filteredInsights(): MatchInsight[] {
    return this.selectedStage
      ? this.insights.filter(i => i.stage === this.selectedStage)
      : this.insights;
  }

  calculatePlayerColumnSize(match: MatchInsight): number {
    let used = 2 + 1 + 1; // Bet, R, Payout

    if (match.showExactResult) used += 1; // Precise result (P)
    if (match.showQualified) used += 1;   // Qualification (Q)

    return 12 - used;
  }

  onAccordionChange(event: Event) {
    const customEvent = event as CustomEvent;
    const expandedMatchId = customEvent.detail?.value;
    this.expandedMatchId = expandedMatchId !== undefined ? Number(expandedMatchId) : null;
  }

  onStageSelected(stage: string) {
    const index = this.availableStages.indexOf(stage);
    if (index !== -1) {
      this.selectedStageIndex = index;
      this.selectedStage = stage;
    }
  }

  prevStage(): void {
    if (this.selectedStageIndex > 0) {
      this.selectedStageIndex -= 1;
      this.selectedStage = this.availableStages[this.selectedStageIndex];
    }
  }

  nextStage(): void {
    if (this.selectedStageIndex < this.availableStages.length - 1) {
      this.selectedStageIndex += 1;
      this.selectedStage = this.availableStages[this.selectedStageIndex];
    }
  }

  getStatusIcon(value?: string | number): string {
    return value === 1 || value === 'V' ? '✔' : value === 0 || value === 'X' ? '✘' : '-';
  }

  getStatusClass(value?: string | number): string {
    return value === 1 || value === 'V' ? 'v-status' : value === 0 || value === 'X' ? 'x-status' : '';
  }
}
