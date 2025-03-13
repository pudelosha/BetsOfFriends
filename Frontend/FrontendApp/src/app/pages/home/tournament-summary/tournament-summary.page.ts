import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonItem } from "@ionic/angular/standalone";
import { IonicModule, ToastController, LoadingController } from '@ionic/angular';


interface Player {
  name: string;
  points: number;
}

@Component({
  selector: 'app-tournament-summary',
  standalone: true,
  imports: [CommonModule, IonicModule],
  templateUrl: './tournament-summary.page.html',
  styleUrls: ['./tournament-summary.page.scss']
})
export class TournamentSummaryPage implements OnInit {
  tournamentSummary = [
    { position: 1, name: 'Johny', points: 20 },
    { position: 2, name: 'Paul', points: 15 },
    { position: 3, name: 'Marcin', points: 12, isCurrentUser: true },
    { position: 4, name: 'Johny', points: 10 },
    { position: 5, name: 'Waclav', points: 5 }
  ];

  constructor() {}

  ngOnInit() {}
}
