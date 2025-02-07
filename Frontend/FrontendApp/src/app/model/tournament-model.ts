export interface Team {
    id: number;
    name: string;
  }
  
  export interface Match {
    matchId: number;
    stage: string;
    homeTeamId: number | null;
    homeTeam: string;
    awayTeamId: number | null;
    awayTeam: string;
    date: string;
    betType: string;
    homeWinOdds: number;
    drawOdds: number;
    awayWinOdds: number;
    homeQualifies: number;
    awayQualifies: number;
  }
  
  export interface Tournament {
    id?: number; // Optional for new tournaments
    tournamentName: string;
    createdBy: string;
    createdAt: string;
    teams: Team[];
    matches: Match[];
  }
  