import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { IonContent, IonHeader, IonTitle, IonToolbar } from '@ionic/angular/standalone';

@Component({
  selector: 'app-summary-dashboard',
  templateUrl: './summary-dashboard.page.html',
  styleUrls: ['./summary-dashboard.page.scss'],
  standalone: true,
  imports: [CommonModule, FormsModule]
})
export class SummaryDashboardPage implements OnInit {

  constructor() { }

  ngOnInit() {
  }

}
