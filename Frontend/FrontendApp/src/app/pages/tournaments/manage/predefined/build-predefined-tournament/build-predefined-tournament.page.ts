import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, FormArray, FormControl } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { IonicModule, ToastController } from '@ionic/angular';
import { StageInputTypePage } from '../../stages/stage-input-type/stage-input-type.page';
import { StageTeamsManagementPage } from '../../stages/stage-teams-management/stage-teams-management.page';
import { StageMatchesManagementPage } from '../../stages/stage-matches-management/stage-matches-management.page';
import { StageSummaryPage } from '../../stages/stage-summary/stage-summary.page';
import { buildMatchFormGroup } from '..//..//../shared/form-utils';
import { PredefinedTournamentService } from '../../../../../services/predefined-tournament.service';
import { Router } from '@angular/router';
import { Tournament, Team, Match } from '../../../../../model/tournament-model';

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
  tournamentId?: number | null = null; // Optional: null for new tournaments, number for existing ones

  constructor(private fb: FormBuilder, 
    private toastController: ToastController,
    private router: Router,
    private tournamentService: PredefinedTournamentService,
  ) {
    this.tournamentForm = this.fb.group({
      tournamentId: [null],
      tournamentName: ['', [Validators.required, Validators.maxLength(50)]],
      uploadMode: ['append'],
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
    // Step 1: Validate Tournament Data
    if (!this.tournamentForm.value.tournamentName?.trim()) {
      this.showToast('Tournament name is required!', 'danger');
      return;
    }
  
    if (this.teamsArray.length < 2) {
      this.showToast('At least 2 teams are required to create a tournament!', 'danger');
      return;
    }
  
    if (this.matchesArray.length < 1) {
      this.showToast('At least 1 match is required!', 'danger');
      return;
    }
  
    // Step 2: Prepare Tournament Data
    const isEditing = !!this.tournamentId; // Determine if editing or creating a new tournament
  
    const tournamentData: Tournament = {
      tournamentId: isEditing ? this.tournamentId : null, // Set ID only if updating
      tournamentName: this.tournamentForm.value.tournamentName,
      isActive: true,
      createdBy: this.tournamentForm.value.createdBy || 'Admin', // Use default if missing
      createdAt: this.tournamentForm.value.createdAt || new Date().toISOString(),
      
      // Step 2.1: Format Teams
      teams: this.teamsArray.value.map((teamName: string) => ({
        teamName, // Only name, no ID (backend generates it for new tournaments)
      })),
  
      // Step 2.2: Format Matches
      matches: this.matchesArray.value.map((match: any) => ({
        matchId: isEditing ? match.matchId || null : null, // Only include matchId if updating
        stage: match.stage || '', // Default to empty if missing
        homeTeamId: isEditing ? match.homeTeamId || null : null, // Only include IDs if editing
        awayTeamId: isEditing ? match.awayTeamId || null : null,
        homeTeam: match.homeTeam,
        awayTeam: match.awayTeam,
        betType: match.betType || '90min', // Ensure betType is always present
        matchStart: new Date(match.matchStart).toISOString(), // Ensure ISO format
        homeWinOdds: match.homeWinOdds,
        drawOdds: match.drawOdds,
        awayWinOdds: match.awayWinOdds,
        homeQualifies: match.homeQualifies,
        awayQualifies: match.awayQualifies,
      })),
    };
  
    console.log('Finalized Tournament Data:', tournamentData);
  
    // Step 3: Determine API Call (Create or Update)
    const submitObservable = isEditing
      ? this.tournamentService.updatePredefinedTournament(tournamentData) // Update
      : this.tournamentService.createPredefinedTournament(tournamentData); // Create
  
    // Step 4: Submit Tournament to Backend
    submitObservable.subscribe({
      next: () => {
        this.router.navigate(['/predefined-tournaments']).then(() => {
          setTimeout(() => {
            this.showToast('Tournament saved successfully!', 'success');
          }, 500); // Small delay to ensure UI update
        });
      },
      error: (error) => {
        console.error('Error submitting tournament:', error);
        this.showToast('Error submitting tournament!', 'danger');
      },
    });
  }
    
  async nextStep() {
    if (await this.canProceed()) {
      this.step++;
    }
  }

  prevStep() {
    if (this.step > 1) {
      this.step--;
    }
  }

  async showToast(message: string, color: 'success' | 'warning' | 'danger' | 'primary'): Promise<void> {
    const toast = await this.toastController.create({
      message,
      duration: 3000,
      position: 'bottom',
      color,
    });
    await toast.present();
  }
  

  async canProceed(): Promise<boolean> {
    switch (this.step) {
      case 1:
        if (!this.tournamentForm.get('tournamentName')?.valid) {
          await this.showToast('Tournament Name is required!', 'danger');
          return false;
        }
        return true;
  
      case 2:
        if (this.teamsArray.length <= 1) {
          await this.showToast('At least 2 teams are required!', 'danger');
          return false;
        }
        return true;
  
      case 3:
        if (this.matchesArray.length === 0) {
          await this.showToast('At least 1 match is required!', 'danger');
          return false;
        }
        return true;
  
      case 4:
        return true; // No validation needed for summary
  
      default:
        return false;
    }
  } 
}
