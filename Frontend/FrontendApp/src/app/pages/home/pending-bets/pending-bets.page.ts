import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonHeader } from "@ionic/angular/standalone";

@Component({
  selector: 'app-pending-bets', // This matches the component tag in HTML
  standalone: true,
  imports: [CommonModule],
  templateUrl: './pending-bets.page.html',
  styleUrls: ['./pending-bets.page.scss']
})
export class PendingBetsPage implements OnInit {
  upcomingGames = [
    { homeTeam: 'Team A', awayTeam: 'Team B', matchTime: '2025-03-01 18:00' },
    { homeTeam: 'Team C', awayTeam: 'Team D', matchTime: '2025-03-02 15:30' },
    { homeTeam: 'Team E', awayTeam: 'Team F', matchTime: '2025-03-03 20:00' },
    { homeTeam: 'Team G', awayTeam: 'Team H', matchTime: '2025-03-04 19:45' },
    { homeTeam: 'Team I', awayTeam: 'Team J', matchTime: '2025-03-05 17:15' }
  ];

  constructor() {}

  ngOnInit() {
    console.log('Upcoming games:', this.upcomingGames);
  }
}
