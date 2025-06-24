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
  matchDateTime: string;
  matchStatus: 'Upcoming' | 'InProgress' | 'Finalized';
  showExactResult: boolean;
  showQualified: boolean;
  userBets: MatchUserBet[];
}

export interface MatchUserBet {
  playerName: string;
  betScore: string;
  resultSuccess: 0 | 1;
  preciseResultSuccess?: 0 | 1;      // optional depending on showExactResult
  qualificationSuccess?: 0 | 1;      // optional depending on showQualified
  totalPayout: number;
}