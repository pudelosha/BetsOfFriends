export interface Bet {
  match: {
    teamHome: string;
    teamAway: string;
    startTime: string;
  };
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
}
