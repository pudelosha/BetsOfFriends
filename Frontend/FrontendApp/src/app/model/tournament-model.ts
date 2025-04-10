export interface Team {
  teamFrontendId: string;  // Unique identifier for frontend tracking
  externalTeamId?: number | null; // To track API data

  teamId: number | null;
  predefinedTeamId?: number | null;
  teamName: string;
  recordStatus: RecordStatus;
}

export interface Match {
  matchFrontendId: string; // Unique frontend match identifier
  matchId: number | null; // Backend match identifier (null for new matches)
  externalMatchId?: number | null;  // To track API data

  predefinedMatchId?: number | null;

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

  matchStatus?: 'Scheduled' | 'Timed' | 'In_Play' | 'Paused' | 'Finished' | 'Postponed' | 'Suspended' | 'Canceled' | null;
  scoreHome?: number | null;
  scoreAway?: number | null;

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
  predefinedStageId?: number | null;
  
  order: number;
  stageName: string;

  recordStatus: RecordStatus;
}

export interface Tournament {
  tournamentId?: number | null;
  externalTournamentId?: number | null;  // To track API data
  season?: number | null;                // e.g. 2024
  seasonId?: number | null;              // API-specific season ID
  tournamentStart?: string | null;       // ISO string of start date
  tournamentEnd?: string | null;         // ISO string of end date
  predefinedTournamentId?: number | null;
  tournamentName: string;
  publicTournamentName?: string;
  tournamentVisibility: 'Private' | 'Public';
  updateMethod: 'Manual' | 'Semi' | 'Auto';
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
  showExactResult: boolean | false;
  showQualified: boolean | false;
  matchesCount: number;
  finalisedMatchesCount: number;

  position: number;
  userId: string;
  userName: string;
  totalBetsPlaced: number;
  betSuccessRate: number;
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
  showExactResult: boolean | false;
  showQualified: boolean | false;

  matchId: number;
  matchStatus: string;
  stage: string;
  homeTeam: string;
  awayTeam: string;

  // User's bet
  betPlaced?: string | null;              // e.g., "2:1" or null if not placed
  whoQualifiedBet?: string | null;        // e.g., "Home", "Away", or null

  // Actual match result
  matchResult: string | null;            // e.g., "2:1" or null if not finalised
  whoQualifiedResult: string | null;     // e.g., "Home", "Away", or null

  // Outcome statuses
  outcomeRegular?: string;
  outcomeQualification?: string;
  outcomeExactResult?: string;

  // Payouts
  payoutRegular?: number;
  payoutQualification?: number;
  payoutExactResult?: number;

  totalPayout?: number;
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

export type RecordStatus = 'New' | 'Uploaded' | 'Update' | 'Delete' | 'Finalised';





