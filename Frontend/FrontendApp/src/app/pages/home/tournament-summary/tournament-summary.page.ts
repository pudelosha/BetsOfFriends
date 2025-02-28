import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';

interface Player {
  name: string;
  points: number;
}

@Component({
  selector: 'app-tournament-summary',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './tournament-summary.page.html',
  styleUrls: ['./tournament-summary.page.scss']
})
export class TournamentSummaryPage implements OnInit {
  userName: string = 'Marcin'; // Set logged-in user dynamically
  rankings: Player[] = [
    { name: 'Johny', points: 20 },
    { name: 'Paul', points: 15 },
    { name: 'Marcin', points: 12 }, // Logged-in user
    { name: 'Johny', points: 10 },
    { name: 'Waclav', points: 5 },
    { name: 'Steve', points: 2 } // Extra user to test visibility logic
  ];

  topPlayers: Player[] = []; // Explicitly declare as empty array
  userRank: number | null = null; // Allow number or null

  constructor() {}

  ngOnInit() {
    // Find user in rankings
    this.userRank = this.rankings.findIndex(player => player.name === this.userName);

    // Ensure logged-in user is always included in topPlayers
    if (this.userRank !== -1 && this.userRank >= 5) {
      this.topPlayers = [...this.rankings.slice(0, 4), this.rankings[this.userRank]];
    } else {
      this.topPlayers = this.rankings.slice(0, 5);
    }

    console.log('Top Players:', this.topPlayers);
  }
}
