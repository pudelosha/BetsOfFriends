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

  homeOdds: number;
  drawOdds: number;
  awayOdds: number;

  qualifyHomeOdds?: number | null;
  qualifyAwayOdds?: number | null;

  qualifiedTeam?: 'Home' | 'Away' | null;

  status: 'ToPlace' | 'Placed' | 'Finalised';
  result: 'Pending' | 'Won' | 'Lost';
}

export interface BetUpdateDto {
  baseAmount: number;
  bonusAmount?: number | null;
  homeGoals?: number | null;
  awayGoals?: number | null;
  qualifiedTeam?: 'Home' | 'Away' | null;
}

export interface AggregatedBet {
  matchId: number;
  
  teamHome: string;
  teamAway: string;
  matchStart: string;

  playerHomeGoals?: number | null;
  playerAwayGoals?: number | null;
  
  percentageHomeWin: number; // % of users betting on home win
  percentageDraw: number;     // % of users betting on draw
  percentageAwayWin: number;  // % of users betting on away win
}


