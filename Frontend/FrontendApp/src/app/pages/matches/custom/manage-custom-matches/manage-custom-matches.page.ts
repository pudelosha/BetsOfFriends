import { Component, CUSTOM_ELEMENTS_SCHEMA, AfterViewInit, OnInit, ViewChild } from '@angular/core';
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

@Component({
  selector: 'app-manage-custom-matches',
  templateUrl: './manage-custom-matches.page.html',
  styleUrls: ['./manage-custom-matches.page.scss'],
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, ManageCustomFinalisedPage, ManageCustomStartedPage, ManageCustomUpcomingPage, TranslateModule, IonSegment, IonSegmentButton, IonGrid, IonRow, IonCol, IonButton, IonIcon, IonSelect, IonSelectOption],
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
})
export class ManageCustomMatchesPage implements OnInit, AfterViewInit {
  selectedTab: string = 'upcoming';
  availableStages: string[] = [];
  selectedStageIndex = 0;
  selectedStage: string = '';

  @ViewChild(IonContent, { static: false }) content!: IonContent;

  constructor(
    private tournamentService: CustomTournamentService,
    private tournamentSelectionService: TournamentSelectionService,
    private route: ActivatedRoute,
    private titleService: TitleService
  ) {}

  async ngOnInit() {
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
  
  ngAfterViewInit() {

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
  
  changeTab(tab: string) {
    this.selectedTab = tab;
    this.scrollToTop();
  }

  scrollToTop() {
    if (this.content) {
      this.content.scrollToTop(300);
    }
  }
}
