import { Component, CUSTOM_ELEMENTS_SCHEMA, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { IonContent } from '@ionic/angular';
import { ManageCustomFinalisedPage } from '../manage-custom-finalised/manage-custom-finalised.page';
import { ManageCustomStartedPage } from '../manage-custom-started/manage-custom-started.page';
import { ManageCustomUpcomingPage } from '../manage-custom-upcoming/manage-custom-upcoming.page';
import { FormsModule } from '@angular/forms';
import { TournamentSelectionService } from 'src/app/services/tournament-selection.service';
import { firstValueFrom } from 'rxjs';
import { CustomTournamentService } from 'src/app/services/custom-tournament.service';
import { ActivatedRoute } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { TitleService } from 'src/app/services/title.service';
import { IonSegment, IonSegmentButton, IonGrid, IonRow, IonCol, IonButton, IonIcon, IonSelect, IonSelectOption } from '@ionic/angular/standalone';
import { CustomMatchService } from 'src/app/services/custom-match.service';

@Component({
  selector: 'app-manage-custom-matches',
  templateUrl: './manage-custom-matches.page.html',
  styleUrls: ['./manage-custom-matches.page.scss'],
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, ManageCustomFinalisedPage, ManageCustomStartedPage, ManageCustomUpcomingPage, TranslateModule, IonSegment, IonSegmentButton, IonGrid, IonRow, IonCol, IonButton, IonIcon, IonSelect, IonSelectOption],
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
})
export class ManageCustomMatchesPage implements OnInit {
  selectedTab: string = 'upcoming';
  availableStages: string[] = [];
  selectedStageIndex = 0;
  selectedStage: string = '';
  private tabChangeSequence = 0;

  @ViewChild(IonContent, { static: false }) content!: IonContent;

  constructor(
    private tournamentService: CustomTournamentService,
    private tournamentSelectionService: TournamentSelectionService,
    private route: ActivatedRoute,
    private titleService: TitleService,
    private matchService: CustomMatchService
  ) {}

  ngOnInit() {
    this.titleService.setTitle('MANAGE_CUSTOM_MATCHES.TITLE');
  }

  ionViewDidEnter() {
    this.titleService.setTitle('MANAGE_CUSTOM_MATCHES.TITLE');
  
    const urlTab = this.route.snapshot.queryParamMap.get('tab');
    const urlStage = this.route.snapshot.queryParamMap.get('stage');
    const urlTournamentId = this.route.snapshot.queryParamMap.get('tournamentId');
  
    if (urlTab === 'upcoming' || urlTab === 'started' || urlTab === 'finalised') {
      this.selectedTab = urlTab;
    } else {
      this.selectedTab = 'upcoming';
    }
  
    if (urlTournamentId && !isNaN(+urlTournamentId)) {
      const parsedId = Number(urlTournamentId);
      this.tournamentSelectionService.setSelectedTournament(parsedId);
    }
  
    this.loadStages(urlStage ?? undefined);
  }
  
  forceTabReload() {
    const currentTab = this.selectedTab;
    this.selectedTab = '';
    setTimeout(() => {
      this.selectedTab = currentTab;
    }, 100);
  }

  async loadStages(stageFromUrl?: string) {
    const tournamentId = this.tournamentSelectionService.getSelectedTournament();
  
    if (!tournamentId) {
      console.warn("No tournament selected.");
      return;
    }
  
    try {
      this.availableStages = await firstValueFrom(this.tournamentService.getTournamentStages(tournamentId));
  
      if (this.availableStages.length > 0) {
        if (stageFromUrl && this.availableStages.includes(stageFromUrl)) {
          this.selectedStage = stageFromUrl;
          this.selectedStageIndex = this.availableStages.indexOf(stageFromUrl);
        } else {
          const stageWithUpcoming = await firstValueFrom(
            this.tournamentService.getFirstStageWithUpcomingMatches(tournamentId)
          );
          
          if (stageWithUpcoming && this.availableStages.includes(stageWithUpcoming)) {
            this.selectedStage = stageWithUpcoming;
            this.selectedStageIndex = this.availableStages.indexOf(stageWithUpcoming);
          } else {
            this.selectedStage = this.availableStages[0];
            this.selectedStageIndex = 0;
          }          
        }

        if (!stageFromUrl) {
          await this.selectFirstStageWithMatchesIfCurrentStageIsEmpty(this.selectedTab);
        }
      }
  
      this.forceTabReload();
    } catch (error) {
      console.error("Error fetching tournament stages:", error);
    }
  }
  
  prevStage() {
    if (this.selectedStageIndex > 0) {
      this.selectedStageIndex--;
      this.selectedStage = this.availableStages[this.selectedStageIndex];
    }
  }
  
  nextStage() {
    if (this.selectedStageIndex < this.availableStages.length - 1) {
      this.selectedStageIndex++;
      this.selectedStage = this.availableStages[this.selectedStageIndex];
    }
  }

  onStageSelected(selected: string) {
    const index = this.availableStages.indexOf(selected);
    if (index !== -1) {
      this.selectedStageIndex = index;
      this.selectedStage = selected;
    }
  }  
  
  async changeTab(tab: string) {
    const requestedTab = this.normalizeTab(tab);
    const sequence = ++this.tabChangeSequence;

    this.selectedTab = '';

    await this.selectFirstStageWithMatchesIfCurrentStageIsEmpty(requestedTab);

    if (sequence !== this.tabChangeSequence) {
      return;
    }

    setTimeout(() => {
      if (sequence === this.tabChangeSequence) {
        this.selectedTab = requestedTab;
        this.scrollToTop();
      }
    }, 100);
  }

  scrollToTop() {
    if (this.content) {
      this.content.scrollToTop(300);
    }
  }

  private normalizeTab(tab: string): string {
    return tab === 'started' || tab === 'finalised' || tab === 'upcoming'
      ? tab
      : 'upcoming';
  }

  private getMatchStatusForTab(tab: string): 'Timed' | 'In_Play' | 'Finished' | null {
    switch (tab) {
      case 'upcoming':
        return 'Timed';
      case 'started':
        return 'In_Play';
      case 'finalised':
        return 'Finished';
      default:
        return null;
    }
  }

  private async selectFirstStageWithMatchesIfCurrentStageIsEmpty(tab: string): Promise<void> {
    const status = this.getMatchStatusForTab(tab);
    const tournamentId = this.tournamentSelectionService.getSelectedTournament();

    if (!status || !tournamentId || !this.availableStages.length || !this.selectedStage) {
      return;
    }

    if (await this.stageHasMatches(tournamentId, status, this.selectedStage)) {
      return;
    }

    for (const stage of this.availableStages) {
      if (stage === this.selectedStage) {
        continue;
      }

      if (await this.stageHasMatches(tournamentId, status, stage)) {
        this.selectedStage = stage;
        this.selectedStageIndex = this.availableStages.indexOf(stage);
        return;
      }
    }
  }

  private async stageHasMatches(
    tournamentId: number,
    status: 'Timed' | 'In_Play' | 'Finished',
    stage: string
  ): Promise<boolean> {
    try {
      const matches = await firstValueFrom(
        this.matchService.getMatchesByTournamentStage(tournamentId, status, stage)
      );

      return matches.length > 0;
    } catch (error) {
      console.error(`Error checking custom matches for stage ${stage}:`, error);
      return false;
    }
  }
}
