import { Component, CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { IonicModule } from '@ionic/angular';
import { MyBetsFinalisedPage } from '../my-bets-finalised/my-bets-finalised.page';
import { MyBetsPlacedPage } from '../my-bets-placed/my-bets-placed.page';
import { MyBetsToPlacePage } from '../my-bets-to-place/my-bets-to-place.page';
import { FormsModule } from '@angular/forms';


@Component({
  selector: 'app-my-bets',
  templateUrl: './my-bets.page.html',
  styleUrls: ['./my-bets.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, FormsModule, ReactiveFormsModule, MyBetsFinalisedPage, MyBetsPlacedPage, MyBetsToPlacePage],
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
})
export class MyBetsPage {
  selectedTab: string = 'to-place';

  constructor() {}

  changeTab(tab: string) {
    this.selectedTab = tab;
  }
}
