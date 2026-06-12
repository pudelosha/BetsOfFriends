export interface Bet {
  betId: number;
  matchId: number;
  teamHome: string;
  teamAway: string;
  homeTeamCrestUrl?: string | null;
  awayTeamCrestUrl?: string | null;
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

  matchStatus: string;
  status: 'ToPlace' | 'Placed' | 'Closed';
  result: 'Pending' | 'Won' | 'Lost';
  type: 'Regular90Min' | 'ExtendedWithQualification'

  showWhoQualifies: boolean | false;
}

export interface BetUpdateDto {
  baseAmount: number;
  bonusAmount?: number | null;
  homeGoals?: number | null;
  awayGoals?: number | null;
  qualifiedTeam?: 'Home' | 'Away' | null;
}

export interface BetStats {
  showExactResult: boolean | null;
  showQualified: boolean | null;
  matchStatus: string | null;
  homeTeam: string;
  awayTeam: string;
  homeTeamCrestUrl?: string | null;
  awayTeamCrestUrl?: string | null;
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
  placedBetsCount: number;
  participantsCount: number;
  averageHomeGoals?: number | null;
  averageAwayGoals?: number | null;
  result?: '1' | 'X' | '2' | null;
  resultQualified?: 'home' | 'away' | null;
  userBets?: UserBetDetails[] | null;
}

export interface UserBetDetails {
  username: string;
  betScore: string;

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
  matchTime: string;
  stage: string
}





