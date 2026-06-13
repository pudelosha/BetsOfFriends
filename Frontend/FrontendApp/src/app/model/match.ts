export interface Match {
  matchId: number;
  stage: string,
  homeTeam: string;
  awayTeam: string;
  matchStart: string;
  
  homeScore?: number | null;
  awayScore?: number | null;
  
  status: 'Upcoming' | 'InProgress' | 'Finalized';  
  qualifiedTeam?: 'Home' | 'Away' | null;
  
  matchType: 'Regular90Min' | 'ExtendedWithQualification';
  isFinished?: boolean;
}

export interface MatchInsight {
  matchId: number;
  stage: string;
  homeTeam: string;
  awayTeam: string;
  result: string | null;
  homeScoreActual?: number | null;
  awayScoreActual?: number | null;
  matchDateTime: string;
  matchStatus: 'Upcoming' | 'InProgress' | 'Finalized';
  showExactResult: boolean;
  showQualified: boolean;
  userBets: MatchUserBet[];
}

export interface MatchUserBet {
  playerName: string;
  betScore: string;
  homeWinSuccess?: 0 | 1 | null;
  drawSuccess?: 0 | 1 | null;
  awayWinSuccess?: 0 | 1 | null;
  resultSuccess?: 0 | 1 | null;
  preciseResultSuccess?: 0 | 1 | null;      // optional depending on showExactResult
  qualificationSuccess?: 0 | 1 | null;      // optional depending on showQualified
  totalPayout: number;
}
