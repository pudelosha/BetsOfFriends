import { Component, CUSTOM_ELEMENTS_SCHEMA, AfterViewInit, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { IonicModule, IonContent } from '@ionic/angular';
import { ManagePredefinedFinalisedPage } from '../manage-predefined-finalised/manage-predefined-finalised.page';
import { ManagePredefinedStartedPage } from '../manage-predefined-started/manage-predefined-started.page';
import { ManagePredefinedUpcomingPage } from '../manage-predefined-upcoming/manage-predefined-upcoming.page';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { ActivatedRoute } from '@angular/router';
import { PredefinedTournamentService } from 'src/app/services/predefined-tournament.service';

@Component({
  selector: 'app-manage-predefined-matches',
  templateUrl: './manage-predefined-matches.page.html',
  styleUrls: ['./manage-predefined-matches.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, FormsModule, ReactiveFormsModule, ManagePredefinedFinalisedPage, ManagePredefinedStartedPage, ManagePredefinedUpcomingPage],
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
  ) {}

  async ngOnInit() {
    this.selectedTab = 'upcoming';

    this.tournamentId = Number(this.route.snapshot.paramMap.get('tournamentId'));
    if (!this.tournamentId) {
      console.warn('No tournament ID in route.');
      return;
    }

    await this.loadStages();
  }

  ngAfterViewInit() {
    this.selectedTab = 'upcoming';
    setTimeout(() => {
      this.scrollToTop();
    }, 100);
  }

  async loadStages() {
    try {
      this.availableStages = await firstValueFrom(this.tournamentService.getTournamentStages(this.tournamentId));
      if (this.availableStages.length > 0) {
        this.selectedStageIndex = 0;
        this.selectedStage = this.availableStages[0];
      }
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
