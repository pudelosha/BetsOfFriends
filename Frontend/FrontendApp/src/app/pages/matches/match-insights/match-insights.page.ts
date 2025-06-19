import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  IonContent,
  IonHeader,
  IonToolbar,
  IonTitle,
  IonSpinner,
  IonGrid,
  IonRow,
  IonCol,
  IonButton,
  IonIcon,
  IonSelect,
  IonSelectOption,
  IonAccordionGroup,
  IonAccordion,
  IonItem,
  IonLabel,
  IonText,
  IonList
} from '@ionic/angular/standalone';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-match-insights',
  templateUrl: './match-insights.page.html',
  styleUrls: ['./match-insights.page.scss'],
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TranslateModule,
    IonContent,
    IonSpinner,
    IonGrid,
    IonRow,
    IonCol,
    IonButton,
    IonIcon,
    IonSelect,
    IonSelectOption,
    IonAccordionGroup,
    IonAccordion,
    IonItem,
    IonLabel
  ]
})
export class MatchInsightsPage implements OnInit {
  isLoading = true;
  insights: any[] = [];
  expandedMatchId: number | null = null;
  selectedStage: string | null = null;
  availableStages: string[] = [];

  constructor(private translate: TranslateService) {}

  ngOnInit() {
    this.fetchInsights();
  }

  fetchInsights() {
    setTimeout(() => {
      const MOCK_INSIGHTS = [
        {
          matchId: 1,
          stage: 'Group A',
          homeTeam: 'Team A',
          awayTeam: 'Team B',
          result: '2-1',
          totalPayout: 7.5,
          betPlaced: '2-1',
          outcomeRegular: 'V',
          payoutRegular: 5,
          showExactResult: true,
          outcomeExactResult: 'V',
          payoutExactResult: 2.5,
          showQualified: true,
          whoQualifiedBet: 'Team A',
          outcomeQualification: 'X',
          payoutQualification: 0
        },
        {
          matchId: 2,
          stage: 'Group B',
          homeTeam: 'Team C',
          awayTeam: 'Team D',
          result: '0-3',
          totalPayout: 8.25,
          betPlaced: '2-2',
          outcomeRegular: 'X',
          payoutRegular: 0,
          showExactResult: false,
          showQualified: true,
          whoQualifiedBet: 'Team D',
          outcomeQualification: 'V',
          payoutQualification: 8.25
        },
        {
          matchId: 3,
          stage: 'Group A',
          homeTeam: 'Team E',
          awayTeam: 'Team F',
          result: null,
          totalPayout: null,
          betPlaced: null,
          outcomeRegular: null,
          payoutRegular: 0,
          showExactResult: false,
          showQualified: false
        }
      ];

      this.insights = MOCK_INSIGHTS;
      this.availableStages = [...new Set(this.insights.map(i => i.stage))];
      this.selectedStage = this.availableStages[0] ?? null;
      this.isLoading = false;
    }, 1000);
  }

  get filteredInsights() {
    return this.selectedStage
      ? this.insights.filter(i => i.stage === this.selectedStage)
      : this.insights;
  }

  onAccordionChange(event: Event) {
    const customEvent = event as CustomEvent;
    this.expandedMatchId = customEvent.detail?.value ?? null;
  }

  onStageChanged(stage: string | null) {
    this.selectedStage = stage;
  }

  getStatusIcon(value?: string): string {
    return value === 'V' ? '✔' : value === 'X' ? '✘' : '-';
  }

  getStatusClass(value?: string): string {
    return value === 'V' ? 'v-status' : value === 'X' ? 'x-status' : '';
  }

  get selectedStageIndex(): number {
    return this.availableStages.findIndex(stage => stage === this.selectedStage);
  }

  prevStage() {
    const i = this.selectedStageIndex;
    if (i > 0) this.selectedStage = this.availableStages[i - 1];
  }

  nextStage() {
    const i = this.selectedStageIndex;
    if (i < this.availableStages.length - 1) this.selectedStage = this.availableStages[i + 1];
  }
}
