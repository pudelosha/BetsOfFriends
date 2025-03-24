export interface Team {
  teamFrontendId: string;  // Unique identifier for frontend tracking
  teamId: number | null;
  teamName: string;
  recordStatus: RecordStatus;
}

export interface Match {
  matchFrontendId: string; // Unique frontend match identifier
  matchId: number | null; // Backend match identifier (null for new matches)

  stageId: number | null; // Backend ID (if available)
  stageFrontendId: string; // Always set for tracking in frontend
  stageName: string; // Readable name of the stage
  
  homeTeamId: number | null; // Backend ID (if available)
  homeTeamFrontendId: string; // Always set for tracking in frontend
  homeTeam: string;

  awayTeamId: number | null; // Backend ID (if available)
  awayTeamFrontendId: string; // Always set for tracking in frontend
  awayTeam: string;

  matchStart: string;
  matchType: string;
  homeWinOdds: number;
  drawOdds: number;
  awayWinOdds: number;
  homeQualifies: number | null;
  awayQualifies: number | null;

  isVisible: boolean;

  recordStatus: RecordStatus;
}

export interface User {
  assignmentId: number | null;
  userName: string;
  userAdminName: string;
  userEmail: string;
  status: 'New' | 'Invited' | 'Accepted' | 'Banned';
  userRole: 'Player' | 'Admin';

  recordStatus: RecordStatus;
}

export interface Stage{
  stageFrontendId: string;  // Unique identifier for frontend tracking
  stageId: number | null;
  order: number;
  stageName: string;

  recordStatus: RecordStatus;
}

export interface Tournament {
  tournamentId?: number | null;
  tournamentName: string;
  isActive: boolean,
  createdBy: string;
  createdAt: string;
  teams: Team[];
  stages: Stage[];
  matches: Match[];
  users?: User[]; // Optional users array
  settings?: TournamentSettings; // Optional tournament settings
}

export interface TournamentSettings {
  tournamentVisibility: 'Private' | 'Public';
  publicTournamentName?: string;
  updateMethod: 'Manual' | 'Semi' | 'Auto';

  allowExactResultBonus: boolean;
  exactResultBonusCalculation: 'Fixed' | 'Multiplied'; // Stored as string
  exactResultBonus: number | null;

  allowWhoQualifiesBets: boolean;

  allowBetsWithBooster: boolean;
  maxBetBooster: number;
  totalBoosterPool: number | null;

  allowNonSubmittedBetsPenalty: boolean;
  nonSubmittedBetPenalty: number | null;
}

export interface UserActiveTournament {
  tournamentId: number;
  tournamentName: string;
  assignmentId: number;
  userName: string;
  numberOfParticipants: number;
  role: string;  // 'Admin' or 'Guest'
  assignmentStatus: string; // 'New', 'Invited', 'Accepted'
  isVisible: boolean;
}

export interface TournamentSummary {
  position: number;
  userId: string;
  userName: string;
  totalBetsPlaced: number;
  successful1X2Results: number;
  successfulQualifications: number;
  successfulExactResults: number;
  totalPayout: number;
}

export interface TournamentPlayerResult {
  position: number;
  userName: string;
  points: number;
  isCurrentUser: boolean;
}

export interface TournamentInvite {
  tournamentName: string;
  numberOfParticipants: number;
  assignmentStatus: string; // "Invited" or "Accepted"
}

export interface UserBettingStats {
  matchId: number;
  homeTeam: string;
  awayTeam: string;
  betPlaced: string | null;
  betOutcome: string;
  whoQualifiedBet: string | null;
  whoQualifiedResult: string | null;
  payout: number;
}

export interface PublicTournament {
  tournamentId: string;
  tournamentName: string;
  createdAt: string;
  participants: number;
  joinRequested: boolean;
}

export interface TournamentParticipant {
  assignmentId: number;
  userName: string;
  userEmail: string;
  role: string;
}

export type RecordStatus = 'New' | 'Uploaded' | 'Update' | 'Delete';





