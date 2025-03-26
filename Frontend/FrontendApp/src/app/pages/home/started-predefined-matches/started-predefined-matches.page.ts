import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { IonContent, IonHeader, IonTitle, IonToolbar } from '@ionic/angular/standalone';

@Component({
  selector: 'app-started-predefined-matches',
  templateUrl: './started-predefined-matches.page.html',
  styleUrls: ['./started-predefined-matches.page.scss'],
  standalone: true,
  imports: [IonContent, IonHeader, IonTitle, IonToolbar, CommonModule, FormsModule]
})
export class StartedPredefinedMatchesPage implements OnInit {

  constructor() { }

  ngOnInit() {
  }

}
