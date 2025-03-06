import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { IonContent, IonHeader, IonTitle, IonToolbar } from '@ionic/angular/standalone';

@Component({
  selector: 'app-bets-overview-upcoming',
  templateUrl: './bets-overview-upcoming.page.html',
  styleUrls: ['./bets-overview-upcoming.page.scss'],
  standalone: true,
  imports: [IonContent, IonHeader, IonTitle, IonToolbar, CommonModule, FormsModule]
})
export class BetsOverviewUpcomingPage implements OnInit {

  constructor() { }

  ngOnInit() {
  }

}
