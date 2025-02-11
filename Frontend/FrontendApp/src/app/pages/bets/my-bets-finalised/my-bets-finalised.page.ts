import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonicModule } from '@ionic/angular';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { Bet } from '..//..//../model/bet';

@Component({
  selector: 'app-my-bets-finalised',
  templateUrl: './my-bets-finalised.page.html',
  styleUrls: ['./my-bets-finalised.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule, FormsModule],
})
export class MyBetsFinalisedPage implements OnInit {
  bets: Bet[] = [
    { 
      match: { teamHome: 'Manchester United', teamAway: 'Manchester City', startTime: '2025-02-12T20:00:00Z' }, 
      playerHomeGoals: 2, playerAwayGoals: 2,  // Player's Prediction
      actualHomeGoals: 2, actualAwayGoals: 2,  // Actual Game Result
      odds: { home: 2.0, draw: 3.5, away: 1.8 } 
    },
    { 
      match: { teamHome: 'Liverpool', teamAway: 'Everton', startTime: '2025-02-10T19:30:00Z' }, 
      playerHomeGoals: 1, playerAwayGoals: 1, 
      actualHomeGoals: 2, actualAwayGoals: 1, 
      odds: { home: 2.5, draw: 3.8, away: 1.9 } 
    },
    { 
      match: { teamHome: 'Chelsea', teamAway: 'Arsenal', startTime: '2025-02-14T21:00:00Z' }, 
      playerHomeGoals: null, playerAwayGoals: null, // Not predicted
      actualHomeGoals: 1, actualAwayGoals: 0, 
      odds: { home: 1.5, draw: 3.2, away: 2.0 } 
    }
  ];

  constructor() {}

  ngOnInit() {}

  ionViewWillEnter(): void {
    this.scrollToTop();
  }

  private scrollToTop(): void {
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  getBetStatus(bet: Bet): string {
    if (
      bet.playerHomeGoals === null || bet.playerHomeGoals === undefined ||
      bet.playerAwayGoals === null || bet.playerAwayGoals === undefined
    ) {
      return 'Not Predicted';
    }
  
    if (
      bet.actualHomeGoals === null || bet.actualHomeGoals === undefined ||
      bet.actualAwayGoals === null || bet.actualAwayGoals === undefined
    ) {
      return 'Not Finalized';
    }
  
    if (bet.playerHomeGoals === bet.actualHomeGoals && bet.playerAwayGoals === bet.actualAwayGoals) {
      return 'Exact Match';
    }
  
    let playerBetWinner: string;
    let actualMatchWinner: string;
  
    // Determine Player's Bet Outcome
    if (bet.playerHomeGoals > bet.playerAwayGoals) {
      playerBetWinner = 'home';
    } else if (bet.playerHomeGoals < bet.playerAwayGoals) {
      playerBetWinner = 'away';
    } else {
      playerBetWinner = 'draw';
    }
  
    // Determine Actual Match Outcome (With Validity Check)
    if (bet.actualHomeGoals !== null && bet.actualAwayGoals !== null) {
      if (bet.actualHomeGoals > bet.actualAwayGoals) {
        actualMatchWinner = 'home';
      } else if (bet.actualHomeGoals < bet.actualAwayGoals) {
        actualMatchWinner = 'away';
      } else {
        actualMatchWinner = 'draw';
      }
    } else {
      return 'Not Finalized';
    }
  
    return playerBetWinner === actualMatchWinner ? 'Won' : 'Lost';
  }
}
