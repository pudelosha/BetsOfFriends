import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { IonContent, IonHeader, IonToolbar, IonTitle, IonSpinner, IonGrid, IonRow, IonCol, IonButton, IonIcon, IonSelect, IonSelectOption, IonAccordionGroup, IonAccordion, IonItem, IonLabel, IonText } from '@ionic/angular/standalone';
import { TranslateModule, TranslateService } from '@ngx-translate/core';


@Component({
  selector: 'app-match-insights',
  templateUrl: './match-insights.page.html',
  styleUrls: ['./match-insights.page.scss'],
  standalone: true,
  imports: [ CommonModule, FormsModule, TranslateModule, IonContent, IonHeader, IonToolbar, IonTitle, IonSpinner, IonGrid, IonRow, IonCol, IonButton, IonIcon, IonSelect, IonSelectOption, IonAccordionGroup, IonAccordion, IonItem, IonLabel, IonText
]
})
export class MatchInsightsPage implements OnInit {
  isLoading = true;
  matches: any[] = []; // Replace with actual Match model
  expandedMatchId: number | null = null;
  selectedStage: string | null = null;
  availableStages: string[] = [];

  constructor(private translate: TranslateService) {}

  ngOnInit() {
    this.fetchMatches();
  }

  fetchMatches() {
    // Replace with API call
    setTimeout(() => {
      const MOCK_MATCHES = [
        {
          id: 1,
          stage: 'Group A',
          homeTeam: 'Team A',
          awayTeam: 'Team B',
          result: '2-1',
          totalPayout: 7.5,
          playerBets: [
            { betPlaced: '2-1', outcome: 'V', payout: 5 },
            { betPlaced: '1-1', outcome: 'X', payout: 0 }
          ]
        },
        {
          id: 2,
          stage: 'Group B',
          homeTeam: 'Team C',
          awayTeam: 'Team D',
          result: '0-3',
          totalPayout: 8.25,
          playerBets: [
            { betPlaced: '0-3', outcome: 'V', payout: 8.25 },
            { betPlaced: '2-2', outcome: 'X', payout: 0 }
          ]
        }
      ];

      this.matches = MOCK_MATCHES; // Replace with fetched data
      this.availableStages = [...new Set(this.matches.map(m => m.stage))];
      this.selectedStage = this.availableStages[0] ?? null;
      this.isLoading = false;
    }, 1000);
  }

  get filteredMatches() {
    return this.selectedStage
      ? this.matches.filter(m => m.stage === this.selectedStage)
      : this.matches;
  }

  onAccordionChange(event: Event) {
    const customEvent = event as CustomEvent;
    this.expandedMatchId = customEvent.detail?.value ?? null;
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

  blurBeforeOpen() {
    requestAnimationFrame(() => {
      const active = document.activeElement as HTMLElement | null;
      if (active) active.blur();
    });
  }
}
