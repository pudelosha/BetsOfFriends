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

export interface BetStats {
  homeTeam: string;
  awayTeam: string;
  homeScoreUser?: number | null;
  awayScoreUser?: number | null;
  homeScoreActual?: number | null;
  awayScoreActual?: number | null;
  qualifiedTeam?: 'home' | 'away' | null;
  percent1: number;
  percentX: number;
  percent2: number;
  percent1Q?: number | null;
  percent2Q?: number | null;
  result?: '1' | 'X' | '2' | null;
  resultQualified?: 'home' | 'away' | null;
}




