import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonicModule } from '@ionic/angular';
import { FormsModule } from '@angular/forms';
import { BetsOverviewFinalisedPage } from '../bets-overview-finalised/bets-overview-finalised.page';
import { BetsOverviewUpcomingPage } from '../bets-overview-upcoming/bets-overview-upcoming.page';

@Component({
  selector: 'app-bets-overview',
  templateUrl: './bets-overview.page.html',
  styleUrls: ['./bets-overview.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, FormsModule, BetsOverviewUpcomingPage, BetsOverviewFinalisedPage],
})
export class BetsOverviewPage {
  selectedTab: 'upcoming' | 'finalised' = 'upcoming';

  changeTab(tab: 'upcoming' | 'finalised') {
    this.selectedTab = tab;
  }
}