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
