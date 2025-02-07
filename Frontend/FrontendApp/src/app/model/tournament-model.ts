export interface Team {
  teamId: number;
  teamName: string;
}

export interface Match {
  matchId: number;
  stage: string;
  homeTeamId: number | null;
  homeTeam: string;
  awayTeamId: number | null;
  awayTeam: string;
  matchStart: string;
  betType: string;
  homeWinOdds: number;
  drawOdds: number;
  awayWinOdds: number;
  homeQualifies: number;
  awayQualifies: number;
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
