export interface Team {
  teamFrontendId: string;
  externalTeamId?: number | null;

  teamId: number | null;
  predefinedTeamId?: number | null;
  teamName: string;
  recordStatus: RecordStatus;
}

export interface Match {
  matchFrontendId: string;
  matchId: number | null;
  externalMatchId?: number | null;

  predefinedMatchId?: number | null;

  stageId: number | null;
  stageFrontendId: string;
  stageName: string;
  
  homeTeamId: number | null;
  homeTeamFrontendId: string;
  homeTeam: string;

  awayTeamId: number | null;
  awayTeamFrontendId: string;
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
  qualifiedTeam?: 'Home' | 'Away' | null;

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
  stageFrontendId: string;
  stageId: number | null;
  predefinedStageId?: number | null;
  
  order: number;
  stageName: string;

  recordStatus: RecordStatus;
}

export interface Tournament {
  tournamentId?: number | null;
  externalTournamentId?: number | null;
  season?: number | null;
  seasonId?: number | null;
  tournamentStart?: string | null;
  tournamentEnd?: string | null;
  predefinedTournamentId?: number | null;
  tournamentName: string;
  publicTournamentName?: string;
  tournamentVisibility: 'Private' | 'Public';
  updateMethod: 'Manual' | 'Semi' | 'Auto';
  isActive: boolean,
  hasLiveUpdates?: boolean,
  createdBy: string;
  createdAt: string;
  teams: Team[];
  stages: Stage[];
  matches: Match[];
  users?: User[];
  settings?: TournamentSettings;
}

export interface TournamentSettings {
  allowExactResultBonus: boolean;
  exactResultBonusCalculation: 'Fixed' | 'Multiplied';
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
  role: string;
  assignmentStatus: string;
  visibility: string;
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
  assignmentStatus: string;
}

export interface UserBettingStats {
  playerName: string | null;

  showExactResult: boolean | false;
  showQualified: boolean | false;

  matchId: number;
  matchStatus: string;
  stage: string;
  homeTeam: string;
  awayTeam: string;

  betPlaced?: string | null;
  whoQualifiedBet?: string | null;

  matchResult: string | null;
  whoQualifiedResult: string | null;

  outcomeRegular?: string;
  outcomeQualification?: string;
  outcomeExactResult?: string;

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

export interface SelectedTournamentDetails {
  tournamentName: string;
  matchesCount: number;
  finalisedMatchesCount: number;
}

export type RecordStatus = 'New' | 'Uploaded' | 'Update' | 'Delete' | 'Finalised';