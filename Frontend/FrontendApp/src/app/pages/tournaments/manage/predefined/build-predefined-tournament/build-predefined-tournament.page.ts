import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, FormArray, FormControl } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { IonicModule } from '@ionic/angular';
import { StageInputTypePage } from '../../stages/stage-input-type/stage-input-type.page';
import { StageTeamsManagementPage } from '../../stages/stage-teams-management/stage-teams-management.page';
import { StageMatchesManagementPage } from '../../stages/stage-matches-management/stage-matches-management.page';
import { StageSummaryPage } from '../../stages/stage-summary/stage-summary.page';
import { buildMatchFormGroup } from '..//..//../shared/form-utils';

@Component({
  selector: 'app-build-predefined-tournament',
  templateUrl: './build-predefined-tournament.page.html',
  styleUrls: ['./build-predefined-tournament.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, StageInputTypePage, StageTeamsManagementPage, StageMatchesManagementPage, StageSummaryPage],
})
export class BuildPredefinedTournamentPage implements OnInit {
  tournamentForm: FormGroup;
  step = 1;

  constructor(private fb: FormBuilder) {
    this.tournamentForm = this.fb.group({
      tournamentName: ['', [Validators.required, Validators.maxLength(50)]],
      teams: this.fb.array([], Validators.required),
      matches: this.fb.array([]),
    });
  }

  ngOnInit(): void {}

  get teamsArray(): FormArray {
    return this.tournamentForm.get('teams') as FormArray;
  }

  get matchesArray(): FormArray {
    return this.tournamentForm.get('matches') as FormArray;
  }

  handleTeamsExtracted(teams: string[]): void {
    this.teamsArray.clear();
    teams.forEach(team => {
      this.teamsArray.push(new FormControl(team, Validators.required));
    });
    console.log('Extracted Teams:', this.teamsArray.value);
  }

  handleMatchesExtracted(matches: any[]): void {
    this.matchesArray.clear();
    matches.forEach(match => {
      this.matchesArray.push(buildMatchFormGroup(this.fb, match));
    });
    console.log('Extracted Matches:', this.matchesArray.value);
  }
  
  handleTeamsUpdated(updatedTeams: string[]): void {
    this.teamsArray.clear();
    updatedTeams.forEach(team => {
      this.teamsArray.push(new FormControl(team, Validators.required));
    });
    console.log('Updated Teams from Child:', this.teamsArray.value);
  }

  handleMatchesUpdated(updatedMatches: any[]): void {
    this.matchesArray.clear();
    updatedMatches.forEach(match => {
      this.matchesArray.push(buildMatchFormGroup(this.fb, match));
    });
    console.log('Updated Matches from Child:', this.matchesArray.value);
  }

  handleSubmitTournament(): void {
    const finalizedTournament = {
      tournamentName: this.tournamentForm.get('tournamentName')?.value || '',
      teams: this.teamsArray.value.map((team: string) => ({ teamName: team })),
      matches: this.matchesArray.value.map((match: any) => ({
        matchId: match.matchId || null, // Include if editing, null if new
        stage: match.stage,
        homeTeamId: match.homeTeamId || null, // Include if editing, null if new
        awayTeamId: match.awayTeamId || null, // Include if editing, null if new
        homeTeam: match.homeTeam,
        awayTeam: match.awayTeam,
        matchStart: match.matchStart,
        homeWinOdds: match.homeWinOdds,
        drawOdds: match.drawOdds,
        awayWinOdds: match.awayWinOdds,
        homeQualifies: match.homeQualifies,
        awayQualifies: match.awayQualifies,
      })),
    };
  
    console.log('Finalized Tournament Submitted:', finalizedTournament);
  
    // Add API call logic here to send `finalizedTournament` to the backend
  }
    
  nextStep() {
    if (this.step < 4 && this.canProceed()) {
      this.step++;
    }
  }

  prevStep() {
    if (this.step > 1) {
      this.step--;
    }
  }

  canProceed(): boolean {
    // Validate the current step before proceeding
    switch (this.step) {
      case 1:
        return this.tournamentForm.get('tournamentName')?.valid ?? false;
      case 2:
        return this.teamsArray.length > 1; // Require at least 2 teams
      case 3:
        return this.matchesArray.length > 0; // Require at least 1 match
      case 4:
        return true; // No validation needed for summary
      default:
        return false;
    }
  }
}
