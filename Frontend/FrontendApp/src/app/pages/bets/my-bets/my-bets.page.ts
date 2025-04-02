import { Component, CUSTOM_ELEMENTS_SCHEMA, AfterViewInit, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { IonicModule, IonContent } from '@ionic/angular';
import { MyBetsFinalisedPage } from '../my-bets-finalised/my-bets-finalised.page';
import { MyBetsPlacedPage } from '../my-bets-placed/my-bets-placed.page';
import { MyBetsToPlacePage } from '../my-bets-to-place/my-bets-to-place.page';
import { FormsModule } from '@angular/forms';
import { TournamentSelectionService } from 'src/app/services/tournament-selection.service';
import { CustomTournamentService } from 'src/app/services/custom-tournament.service';
import { firstValueFrom } from 'rxjs';
import { ActivatedRoute } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';


@Component({
  selector: 'app-my-bets',
  templateUrl: './my-bets.page.html',
  styleUrls: ['./my-bets.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, FormsModule, ReactiveFormsModule, MyBetsFinalisedPage, MyBetsPlacedPage, MyBetsToPlacePage, TranslateModule],
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
})
export class MyBetsPage implements OnInit, AfterViewInit {
  selectedTab: string = 'to-place'; // Default tab
  availableStages: string[] = [];
  selectedStageIndex = 0;
  selectedStage: string = '';
  
  @ViewChild(IonContent, { static: false }) content!: IonContent; // Get content reference

  constructor(
    private tournamentService: CustomTournamentService,
    private tournamentSelectionService: TournamentSelectionService,
    private route: ActivatedRoute
  ) {}

  ngOnInit() {
    this.loadStages(); // Load tournament stages
  }

  ngAfterViewInit() {

  }

  ionViewDidEnter() {
    const urlTab = this.route.snapshot.queryParamMap.get('tab');
    if (urlTab === 'to-place' || urlTab === 'placed' || urlTab === 'finalised') {
      this.selectedTab = urlTab;
    } else {
      this.selectedTab = 'to-place'; 
    }
    this.forceTabReload();
  }

  triggerRefresh() {
    console.log('Triggering tab refresh...');
    this.changeTab(this.selectedTab);
  }

  changeTab(tab: string) {
    console.log(`Switching to tab: ${tab}`);
    this.selectedTab = ''; // Force unmount
    setTimeout(() => {
      this.selectedTab = tab; // Remount child
    }, 100);
  }

  forceTabReload() {
    console.log(`Force reloading tab: ${this.selectedTab}`);
    const currentTab = this.selectedTab;
    this.selectedTab = ''; // Force reset
    setTimeout(() => {
      this.selectedTab = currentTab; // Restore tab
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
      console.log(`Stage changed to: ${selected}`);
    }
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

  scrollToTop() {
    if (this.content) {
      this.content.scrollToTop(300); // Smooth scroll to top
      console.log('Scrolled to top');
    }
  }
}
