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
  playerQualifiedTeam: 'Home' | 'Away' | 'Neutral' | null;
  actualHomeGoals?: number | null;
  actualAwayGoals?: number | null;
  actualQualifiedTeam?: 'Home' | 'Away' | null;
 
  homeOdds: number;
  drawOdds: number;
  awayOdds: number;
  qualifyHomeOdds?: number | null;
  qualifyAwayOdds?: number | null;

  status: 'ToPlace' | 'Placed' | 'Finalised';
  result: 'Pending' | 'Won' | 'Lost';
  type: 'Regular90Min' | 'ExtendedWithQualification'
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

  /** User bets are optional (can be null if match isn't finished) */
  userBets?: UserBetDetails[] | null;
}

export interface UserBetDetails {
  username: string;
  betScore: string;

  /** 1 = success, 0 = failure, null = not applicable */
  homeWinSuccess?: number | null;
  drawSuccess?: number | null;
  awayWinSuccess?: number | null;
  homeQualifiesSuccess?: number | null;
  awayQualifiesSuccess?: number | null;
  resultSuccess?: number | null;
}

export interface UpcomingBet {
  matchId: number;
  homeTeam: string;
  awayTeam: string;
  matchTime: string; // ISO date string
}





