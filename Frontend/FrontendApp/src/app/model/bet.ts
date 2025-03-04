export interface Bet {
  betId: number;
  matchId: number;
  teamHome: string;
  teamAway: string;
  startTime: string;

  baseAmount: number;
  bonusAmount?: number | null;

  playerHomeGoals?: number | null;
  playerAwayGoals?: number | null;
  actualHomeGoals?: number | null;
  actualAwayGoals?: number | null;

  odds: {
    home: number;
    draw: number;
    away: number;
  };
  qualifyOdds?: {
    home: number;
    away: number;
  };

  qualifiedTeam?: 'Home' | 'Away' | null;

  status: 'ToPlace' | 'Placed' | 'Finalised';
  result: 'Pending' | 'Won' | 'Lost';
}
