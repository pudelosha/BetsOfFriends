import { Component, CUSTOM_ELEMENTS_SCHEMA, AfterViewInit, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { IonicModule, IonContent } from '@ionic/angular';
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
export class ManageMatchesPage implements OnInit, AfterViewInit {
  selectedTab: string = 'upcoming'; // Default tab
  
  @ViewChild(IonContent, { static: false }) content!: IonContent; // Reference to IonContent

  constructor() {}

  ngOnInit() {
    this.selectedTab = 'upcoming'; // Ensure the default tab
  }

  ngAfterViewInit() {
    this.selectedTab = 'upcoming'; // Ensure the default tab
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
