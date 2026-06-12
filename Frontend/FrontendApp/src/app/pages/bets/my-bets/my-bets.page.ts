import { Component, CUSTOM_ELEMENTS_SCHEMA, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { IonContent } from '@ionic/angular';
import { MyBetsFinalisedPage } from '../my-bets-finalised/my-bets-finalised.page';
import { MyBetsPlacedPage } from '../my-bets-placed/my-bets-placed.page';
import { MyBetsToPlacePage } from '../my-bets-to-place/my-bets-to-place.page';
import { FormsModule } from '@angular/forms';
import { TournamentSelectionService } from 'src/app/services/tournament-selection.service';
import { CustomTournamentService } from 'src/app/services/custom-tournament.service';
import { firstValueFrom } from 'rxjs';
import { ActivatedRoute } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { TitleService } from 'src/app/services/title.service';
import { IonSegment, IonSegmentButton, IonGrid, IonRow, IonCol, IonButton, IonIcon, IonSelect, IonSelectOption } from '@ionic/angular/standalone';
import { BetService } from 'src/app/services/bet.service';

@Component({
  selector: 'app-my-bets',
  templateUrl: './my-bets.page.html',
  styleUrls: ['./my-bets.page.scss'],
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, MyBetsFinalisedPage, MyBetsPlacedPage, MyBetsToPlacePage, TranslateModule, IonSegment, IonSegmentButton, IonGrid, IonRow, IonCol, IonButton, IonIcon, IonSelect, IonSelectOption],
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
})
export class MyBetsPage {
  selectedTab: string = 'to-place';
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
    private betService: BetService
  ) {}

  ionViewDidEnter() {
    this.route.queryParamMap.subscribe(params => {
      const urlTab = params.get('tab') ?? 'to-place';
      const urlStage = params.get('stage') ?? undefined;
      const urlTournamentId = params.get('tournamentId');
  
      if (urlTab === 'to-place' || urlTab === 'placed' || urlTab === 'finalised') {
        this.selectedTab = urlTab;
      } else {
        this.selectedTab = 'to-place';
      }
  
      if (urlTournamentId && !isNaN(+urlTournamentId)) {
        const parsedId = Number(urlTournamentId);
        this.tournamentSelectionService.setSelectedTournament(parsedId);
      }
  
      this.loadStages(urlStage);
    });

    this.titleService.setTitle('MY_BETS.TITLE');
  }
     
  triggerRefresh() {
    this.changeTab(this.selectedTab);
  }

  async changeTab(tab: string) {
    const requestedTab = this.normalizeTab(tab);
    const sequence = ++this.tabChangeSequence;

    this.selectedTab = '';

    await this.selectFirstStageWithBetsIfCurrentStageIsEmpty(requestedTab);

    if (sequence !== this.tabChangeSequence) {
      return;
    }

    setTimeout(() => {
      if (sequence === this.tabChangeSequence) {
        this.selectedTab = requestedTab;
      }
    }, 100);
  }

  forceTabReload() {
    const currentTab = this.selectedTab;
    this.selectedTab = '';
    setTimeout(() => {
      this.selectedTab = currentTab;
    }, 100);
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
          const stageWithPending = await firstValueFrom(
            this.tournamentService.getFirstStageWithPendingBets(tournamentId)
          );
          
          if (stageWithPending && this.availableStages.includes(stageWithPending)) {
            this.selectedStage = stageWithPending;
            this.selectedStageIndex = this.availableStages.indexOf(stageWithPending);
          } else {
            this.selectedStage = this.availableStages[0];
            this.selectedStageIndex = 0;
          }          
        }

        if (!stageFromUrl) {
          await this.selectFirstStageWithBetsIfCurrentStageIsEmpty(this.selectedTab);
        }
      }
      
      this.forceTabReload();
    } catch (error) {
      console.error("Error fetching tournament stages:", error);
    }
  }   

  scrollToTop() {
    if (this.content) {
      this.content.scrollToTop(300);
    }
  }

  private normalizeTab(tab: string): string {
    return tab === 'placed' || tab === 'finalised' || tab === 'to-place'
      ? tab
      : 'to-place';
  }

  private getBetStatusForTab(tab: string): 'ToPlace' | 'Placed' | 'Closed' | null {
    switch (tab) {
      case 'to-place':
        return 'ToPlace';
      case 'placed':
        return 'Placed';
      case 'finalised':
        return 'Closed';
      default:
        return null;
    }
  }

  private async selectFirstStageWithBetsIfCurrentStageIsEmpty(tab: string): Promise<void> {
    const status = this.getBetStatusForTab(tab);
    const tournamentId = this.tournamentSelectionService.getSelectedTournament();

    if (!status || !tournamentId || !this.availableStages.length || !this.selectedStage) {
      return;
    }

    if (await this.stageHasBets(tournamentId, status, this.selectedStage)) {
      return;
    }

    for (const stage of this.availableStages) {
      if (stage === this.selectedStage) {
        continue;
      }

      if (await this.stageHasBets(tournamentId, status, stage)) {
        this.selectedStage = stage;
        this.selectedStageIndex = this.availableStages.indexOf(stage);
        return;
      }
    }
  }

  private async stageHasBets(
    tournamentId: number,
    status: 'ToPlace' | 'Placed' | 'Closed',
    stage: string
  ): Promise<boolean> {
    try {
      const bets = await firstValueFrom(
        this.betService.getBetsByTournamentStage(tournamentId, status, stage)
      );

      return bets.length > 0;
    } catch (error) {
      console.error(`Error checking bets for stage ${stage}:`, error);
      return false;
    }
  }
}
