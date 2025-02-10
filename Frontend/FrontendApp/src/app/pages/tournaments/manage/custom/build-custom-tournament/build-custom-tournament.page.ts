import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { IonContent, IonHeader, IonTitle, IonToolbar } from '@ionic/angular/standalone';

@Component({
  selector: 'app-build-custom-tournament',
  templateUrl: './build-custom-tournament.page.html',
  styleUrls: ['./build-custom-tournament.page.scss'],
  standalone: true,
  imports: [IonContent, IonHeader, IonTitle, IonToolbar, CommonModule, FormsModule]
})
export class BuildCustomTournamentPage implements OnInit {

  constructor() { }

  ngOnInit() {
  }

}
