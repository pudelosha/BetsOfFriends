export interface Match {
    matchId: number;
    
    homeTeam: string;
    awayTeam: string;
    matchStart: string;
  
    homeScore?: number | null;
    awayScore?: number | null;
  
    status: 'Upcoming' | 'InProgress' | 'Finalized';  // Match status

    qualifiedTeam?: 'Home' | 'Away' | null; 
  }
  