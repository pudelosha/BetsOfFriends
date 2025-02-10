import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { IonContent, IonHeader, IonTitle, IonToolbar } from '@ionic/angular/standalone';

@Component({
  selector: 'app-live-results-dashboard',
  templateUrl: './live-results-dashboard.page.html',
  styleUrls: ['./live-results-dashboard.page.scss'],
  standalone: true,
  imports: [CommonModule, FormsModule]
})
export class LiveResultsDashboardPage implements OnInit {

  constructor() { }

  ngOnInit() {
  }

}
