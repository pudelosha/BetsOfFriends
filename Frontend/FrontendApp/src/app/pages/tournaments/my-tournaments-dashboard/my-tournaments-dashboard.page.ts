import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { IonContent, IonHeader, IonTitle, IonToolbar } from '@ionic/angular/standalone';

@Component({
  selector: 'app-my-tournaments-dashboard',
  templateUrl: './my-tournaments-dashboard.page.html',
  styleUrls: ['./my-tournaments-dashboard.page.scss'],
  standalone: true,
  imports: [CommonModule, FormsModule]
})
export class MyTournamentsDashboardPage implements OnInit {

  constructor() { }

  ngOnInit() {
  }

}
