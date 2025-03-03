export interface Team {
  teamFrontendId: string;  // Unique identifier for frontend tracking
  teamId: number | null;
  teamName: string;
}

export interface Match {
  matchFrontendId: string; // Unique frontend match identifier
  matchId: number | null; // Backend match identifier (null for new matches)
  stage?: string | null;
  
  homeTeamId: number | null; // Backend ID (if available)
  homeTeamFrontendId: string; // Always set for tracking in frontend
  homeTeam: string;

  awayTeamId: number | null; // Backend ID (if available)
  awayTeamFrontendId: string; // Always set for tracking in frontend
  awayTeam: string;

  matchStart: string;
  betType: string;
  homeWinOdds: number;
  drawOdds: number;
  awayWinOdds: number;
  homeQualifies: number | null;
  awayQualifies: number | null;
}

export interface User {
  assignmentId: number | null;
  userName: string;
  userAdminName: string;
  userEmail: string;
  status: 'New' | 'Invited' | 'Accepted' | 'Banned';
}

export interface Tournament {
  tournamentId?: number | null;
  tournamentName: string;
  isActive: boolean,
  createdBy: string;
  createdAt: string;
  teams: Team[];
  matches: Match[];
  users?: User[]; // Optional users array
}

export interface UserActiveTournament {
  tournamentId: number;
  tournamentName: string;
  assignmentId: number;
  userName: string;
  numberOfParticipants: number;
  role: string;  // 'Admin' or 'Guest'
  assignmentStatus: string; // 'New', 'Invited', 'Accepted'
}

