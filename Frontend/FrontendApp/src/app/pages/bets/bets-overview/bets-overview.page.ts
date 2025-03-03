import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { IonContent, IonHeader, IonTitle, IonToolbar } from '@ionic/angular/standalone';

@Component({
  selector: 'app-bets-overview',
  templateUrl: './bets-overview.page.html',
  styleUrls: ['./bets-overview.page.scss'],
  standalone: true,
  imports: [CommonModule, FormsModule]
})
export class BetsOverviewPage implements OnInit {

  constructor() { }

  ngOnInit() {
  }

}
