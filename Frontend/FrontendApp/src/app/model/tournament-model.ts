export interface Team {
  teamId: number | null;
  teamName: string;
}

export interface Match {
  matchId: number| null;
  stage?: string | null;
  homeTeamId: number | null;
  homeTeam: string;
  awayTeamId: number | null;
  awayTeam: string;
  matchStart: string;
  betType: string;
  homeWinOdds: number;
  drawOdds: number;
  awayWinOdds: number;
  homeQualifies: number | null;
  awayQualifies: number | null;
}

export interface Tournament {
  tournamentId?: number | null;
  tournamentName: string;
  isActive: boolean,
  createdBy: string;
  createdAt: string;
  teams: Team[];
  matches: Match[];
}
