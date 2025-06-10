import { Component, CUSTOM_ELEMENTS_SCHEMA, AfterViewInit, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { IonContent } from '@ionic/angular';
import { ManagePredefinedFinalisedPage } from '../manage-predefined-finalised/manage-predefined-finalised.page';
import { ManagePredefinedStartedPage } from '../manage-predefined-started/manage-predefined-started.page';
import { ManagePredefinedUpcomingPage } from '../manage-predefined-upcoming/manage-predefined-upcoming.page';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { ActivatedRoute } from '@angular/router';
import { PredefinedTournamentService } from 'src/app/services/predefined-tournament.service';
import { TranslateModule } from '@ngx-translate/core';
import { TitleService } from 'src/app/services/title.service';
import { IonSegment, IonSegmentButton, IonGrid, IonRow, IonCol, IonButton, IonIcon, IonSelect, IonSelectOption } from '@ionic/angular/standalone';

@Component({
  selector: 'app-manage-predefined-matches',
  templateUrl: './manage-predefined-matches.page.html',
  styleUrls: ['./manage-predefined-matches.page.scss'],
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, ManagePredefinedFinalisedPage, ManagePredefinedStartedPage, ManagePredefinedUpcomingPage, TranslateModule, IonSegment, IonSegmentButton, IonGrid, IonRow, IonCol, IonButton, IonIcon, IonSelect, IonSelectOption],
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
})
export class ManagePredefinedMatchesPage implements OnInit, AfterViewInit {
  selectedTab: string = 'upcoming';
  availableStages: string[] = [];
  selectedStageIndex = 0;
  selectedStage: string = '';
  tournamentId!: number;

  @ViewChild(IonContent, { static: false }) content!: IonContent;

  constructor(
    private route: ActivatedRoute,
    private tournamentService: PredefinedTournamentService,
    private titleService: TitleService
  ) {}

  async ngOnInit() {
    this.titleService.setTitle('MANAGE_PREDEFINED_MATCHES.TITLE');
    this.selectedTab = 'upcoming';

    this.tournamentId = Number(this.route.snapshot.paramMap.get('tournamentId'));
    if (!this.tournamentId) {
      console.warn('No tournament ID in route.');
      return;
    }

    await this.loadStages();
  }

  ngAfterViewInit() {
    this.titleService.setTitle('MANAGE_PREDEFINED_MATCHES.TITLE');
    this.selectedTab = 'upcoming';
    setTimeout(() => {
      this.scrollToTop();
    }, 100);
  }

  async loadStages(stageFromUrl?: string) {
    try {
      this.availableStages = await firstValueFrom(
        this.tournamentService.getTournamentStages(this.tournamentId)
      );
  
      if (this.availableStages.length > 0) {
        if (stageFromUrl && this.availableStages.includes(stageFromUrl)) {
          this.selectedStage = stageFromUrl;
          this.selectedStageIndex = this.availableStages.indexOf(stageFromUrl);
        } else {
          const stageWithUpcoming = await firstValueFrom(
            this.tournamentService.getFirstStageWithUpcomingMatches(this.tournamentId)
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
      console.error("Error fetching stages:", error);
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

  forceTabReload() {
    const currentTab = this.selectedTab;
    this.selectedTab = '';
    setTimeout(() => {
      this.selectedTab = currentTab;
    }, 100);
  }

  scrollToTop() {
    if (this.content) {
      this.content.scrollToTop(300);
    }
  }
}
