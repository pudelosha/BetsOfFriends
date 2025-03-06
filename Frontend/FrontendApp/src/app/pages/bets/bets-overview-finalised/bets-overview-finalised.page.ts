import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { IonContent, IonHeader, IonTitle, IonToolbar } from '@ionic/angular/standalone';

@Component({
  selector: 'app-bets-overview-finalised',
  templateUrl: './bets-overview-finalised.page.html',
  styleUrls: ['./bets-overview-finalised.page.scss'],
  standalone: true,
  imports: [IonContent, IonHeader, IonTitle, IonToolbar, CommonModule, FormsModule]
})
export class BetsOverviewFinalisedPage implements OnInit {

  constructor() { }

  ngOnInit() {
  }

}
