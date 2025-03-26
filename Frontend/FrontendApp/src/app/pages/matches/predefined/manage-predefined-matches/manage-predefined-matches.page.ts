import { Component, CUSTOM_ELEMENTS_SCHEMA, AfterViewInit, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { IonicModule, IonContent } from '@ionic/angular';
import { ManagePredefinedFinalisedPage } from '../manage-predefined-finalised/manage-predefined-finalised.page';
import { ManagePredefinedStartedPage } from '../manage-predefined-started/manage-predefined-started.page';
import { ManagePredefinedUpcomingPage } from '../manage-predefined-upcoming/manage-predefined-upcomingpage';
import { FormsModule } from '@angular/forms';
import { TournamentSelectionService } from 'src/app/services/tournament-selection.service';
import { firstValueFrom } from 'rxjs';
import { CustomTournamentService } from 'src/app/services/custom-tournament.service';

@Component({
  selector: 'app-manage-predefined-matches',
  templateUrl: './manage-predefined-matches.page.html',
  styleUrls: ['./manage-predefined-matches.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, FormsModule, ReactiveFormsModule, ManagePredefinedFinalisedPage, ManagePredefinedStartedPage, ManagePredefinedUpcomingPage],
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
})
export class ManagePredefinedMatchesPage implements OnInit, AfterViewInit {
  selectedTab: string = 'upcoming'; // Default tab
  availableStages: string[] = [];
  selectedStageIndex = 0;
  selectedStage: string = '';

  @ViewChild(IonContent, { static: false }) content!: IonContent;

  constructor(
    private tournamentService: CustomTournamentService,
    private tournamentSelectionService: TournamentSelectionService
  ) {}

  async ngOnInit() {
    this.selectedTab = 'upcoming';
    await this.loadStages(); // Load tournament stages
  }

  ngAfterViewInit() {
    this.selectedTab = 'upcoming';
    setTimeout(() => {
      this.scrollToTop();
    }, 100);
  }

  async loadStages() {
    const tournamentId = this.tournamentSelectionService.getSelectedTournament();
    
    if (!tournamentId) {
      console.warn("No tournament selected.");
      return;
    }

    try {
      this.availableStages = await firstValueFrom(this.tournamentService.getTournamentStages(tournamentId));
      if (this.availableStages.length > 0) {
        this.selectedStageIndex = 0;
        this.selectedStage = this.availableStages[0]; // Set the first stage as default
      }
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
  
  changeTab(tab: string) {
    this.selectedTab = tab;
    this.scrollToTop();
  }

  scrollToTop() {
    if (this.content) {
      this.content.scrollToTop(300);
      console.log('Scrolled to top');
    }
  }
}
