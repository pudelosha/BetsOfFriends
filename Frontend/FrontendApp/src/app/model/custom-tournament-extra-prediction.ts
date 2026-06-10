export interface CustomTournamentExtraPredictionTeamOption {
  teamId: number;
  teamName: string;
}

export interface CustomTournamentExtraPrediction {
  tournamentId: number;
  userName: string;
  isCurrentUser: boolean;
  hasPrediction: boolean;
  winnerTeamId: number | null;
  secondPlaceTeamId: number | null;
  thirdPlaceTeamId: number | null;
  topScorerTeamId: number | null;
  topScorerName: string;
  updatedAt: string | null;
}

export interface CustomTournamentExtraPredictionsOverview {
  tournamentId: number;
  isLocked: boolean;
  teams: CustomTournamentExtraPredictionTeamOption[];
  predictions: CustomTournamentExtraPrediction[];
}

export interface CustomTournamentExtraPredictionFormValue {
  winnerTeamId: number | null;
  secondPlaceTeamId: number | null;
  thirdPlaceTeamId: number | null;
  topScorerTeamId: number | null;
  topScorerName: string;
}
