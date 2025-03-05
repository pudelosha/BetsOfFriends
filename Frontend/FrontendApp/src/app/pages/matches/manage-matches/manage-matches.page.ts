import { Component, CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { IonicModule } from '@ionic/angular';
import { ManageFinalisedPage } from '../manage-finalised/manage-finalised.page';
import { ManageStartedPage } from '../manage-started/manage-started.page';
import { ManageUpcomingPage } from '../manage-upcoming/manage-upcoming.page';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-manage-matches',
  templateUrl: './manage-matches.page.html',
  styleUrls: ['./manage-matches.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, FormsModule, ReactiveFormsModule, ManageFinalisedPage, ManageStartedPage, ManageUpcomingPage],
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
})
export class ManageMatchesPage {
  selectedTab: string = 'upcoming';

  constructor() {}

  changeTab(tab: string) {
    this.selectedTab = tab;
  }
}
