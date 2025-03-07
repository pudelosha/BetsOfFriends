import { Component, CUSTOM_ELEMENTS_SCHEMA, AfterViewInit, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { IonicModule, IonContent } from '@ionic/angular';
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
export class MyBetsPage implements OnInit, AfterViewInit {
  selectedTab: string = 'to-place'; // Default tab
  
  @ViewChild(IonContent, { static: false }) content!: IonContent; // Get content reference

  constructor() {}

  ngOnInit() {
    this.selectedTab = 'to-place'; // Ensure the default tab
  }

  ngAfterViewInit() {
    this.selectedTab = 'to-place'; // Ensure the default tab
    setTimeout(() => {
      this.scrollToTop();
    }, 100);
  }

  changeTab(tab: string) {
    this.selectedTab = tab;
    this.scrollToTop();
  }

  scrollToTop() {
    if (this.content) {
      this.content.scrollToTop(300); // Smooth scroll to top
      console.log('Scrolled to top');
    }
  }
}
