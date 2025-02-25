import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, FormArray, FormControl, AbstractControl } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { IonicModule, ToastController } from '@ionic/angular';
import { StageInputTypePage } from '../../stages/stage-input-type/stage-input-type.page';
import { StageTeamsManagementPage } from '../../stages/stage-teams-management/stage-teams-management.page';
import { StageMatchesManagementPage } from '../../stages/stage-matches-management/stage-matches-management.page';
import { StageSummaryPage } from '../../stages/stage-summary/stage-summary.page';
import { buildMatchFormGroup } from '../../../shared/form-utils';
import { PredefinedTournamentService } from 'src/app/services/predefined-tournament.service';
import { Router, ActivatedRoute } from '@angular/router';
import { Tournament, Team, Match } from '../../../../../model/tournament-model';
import { EditMatchModalComponent } from 'src/app/modals/edit-match-modal/edit-match-modal.component';
import { EditTeamModalComponent } from 'src/app/modals/edit-team-modal/edit-team-modal.component';
import { ModalController } from '@ionic/angular';
import { ViewChild } from '@angular/core';

@Component({
  selector: 'app-build-predefined-tournament',
  templateUrl: './build-predefined-tournament.page.html',
  styleUrls: ['./build-predefined-tournament.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, StageInputTypePage, StageTeamsManagementPage, StageMatchesManagementPage, StageSummaryPage],
})
export class BuildPredefinedTournamentPage implements OnInit {
  @ViewChild(StageTeamsManagementPage) stageTeamsManagement!: StageTeamsManagementPage;
  @ViewChild(StageMatchesManagementPage) stageMatchesManagement!: StageMatchesManagementPage;

  tournamentForm: FormGroup;
  step = 1;
  tournamentId?: number | null = null; // Optional: null for new tournaments, number for existing ones
  isLoading = false;

  constructor(private fb: FormBuilder, 
    private toastController: ToastController,
    private router: Router,
    private route: ActivatedRoute,
    private tournamentService: PredefinedTournamentService,
    private modalController: ModalController
  ) {
    this.tournamentForm = this.fb.group({
      tournamentId: [null],
      tournamentName: ['', [Validators.required, Validators.maxLength(50)]],
      importMethod: ['upload'],
      teams: this.fb.array([], Validators.required),  // Holds Team models
      matches: this.fb.array([]), // Holds Match models
    });
  }

  ngOnInit(): void {
    this.route.paramMap.subscribe((params) => {
      const id = params.get('id');
      if (id && !isNaN(+id)) {
        this.tournamentId = +id; // Convert the id to a number
        this.loadTournament();
      } else {
        this.tournamentId = null;
      }
    });
  }

  ionViewWillEnter(): void {
    this.resetFormData();
    this.scrollToTop();
    this.step = 1;
  }

  private resetFormData(): void {
    this.tournamentForm.reset();
    this.teamsArray.clear();
    this.matchesArray.clear();
    this.tournamentForm.get('importMethod')?.setValue('upload');
  }

  private scrollToTop(): void {
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  get teamsArray(): FormArray {
    return this.tournamentForm.get('teams') as FormArray;
  }

  get matchesArray(): FormArray {
    return this.tournamentForm.get('matches') as FormArray;
  }

  async openAddModal(): Promise<void> {
    switch (this.step) {
      case 2:
        if (this.stageTeamsManagement) {
          await this.stageTeamsManagement.addTeam();
        } else {
          console.warn('StageTeamsManagementPage reference is not available.');
        }
        break;
      case 3:
        if (this.stageMatchesManagement) {
          await this.stageMatchesManagement.addMatch();
        } else {
          console.warn('StageMatchesManagement reference is not available.');
        }
        break;
      default:
        console.warn('Invalid step for adding data:', this.step);
    }
  }

  private loadTournament(): void {
    if (!this.tournamentId) {
      console.error('Tournament ID is missing.');
      return;
    }
  
    this.tournamentService.getPredefinedTournamentById(this.tournamentId).subscribe({
      next: (tournament) => {
        if (tournament) {
          this.populateForm(tournament);
        } else {
          console.error('Tournament not found:', this.tournamentId);
        }
      },
      error: (err) => {
        console.error('Error loading tournament:', err);
      },
    });
  }  
  
  private populateForm(tournament: Tournament): void {
    this.tournamentForm.patchValue({
      tournamentId: tournament.tournamentId,
      tournamentName: tournament.tournamentName,
      importMethod: 'upload',
    });
  
    // Populate Teams
    this.teamsArray.clear();
    tournament.teams.forEach((team) => {
      this.teamsArray.push(this.fb.group({
        frontendId: [team.frontendId],
        backendId: [team.backendId], 
        teamName: [team.teamName, Validators.required],
      }));
    });
  
    // Populate Matches
    this.matchesArray.clear();
    tournament.matches.forEach((match) => {
      this.matchesArray.push(this.buildMatchFormGroup(match));
    });
  }

  private buildMatchFormGroup(match: Match): FormGroup {
    return this.fb.group({
      frontendId: [match.frontendId],  // Match frontend ID
      backendId: [match.backendId],    // Backend ID (null if new)
  
      stage: [match.stage || null],
      
      homeTeamId: [match.homeTeamId],  // Backend ID
      homeTeamFrontendId: [match.homeTeamFrontendId],  // Ensure frontend ID is stored
      homeTeam: [match.homeTeam],  
  
      awayTeamId: [match.awayTeamId],  // Backend ID
      awayTeamFrontendId: [match.awayTeamFrontendId],  // Ensure frontend ID is stored
      awayTeam: [match.awayTeam],  
  
      matchStart: [match.matchStart],
      betType: [match.betType || '90min'],
      homeWinOdds: [match.homeWinOdds ?? 0],
      drawOdds: [match.drawOdds ?? 0],
      awayWinOdds: [match.awayWinOdds ?? 0],
      homeQualifies: [match.homeQualifies ?? null],
      awayQualifies: [match.awayQualifies ?? null],
    });
  }
          
  handleTeamsExtracted(teams: Team[]): void {
    const importMethod = this.tournamentForm.get('importMethod')?.value;
  
    console.log(importMethod);
  
    if (importMethod === 'upload') {
      // Replace all teams
      this.teamsArray.clear();
      teams.forEach((team) => {
        this.teamsArray.push(this.fb.group({
          frontendId: [team.frontendId],
          backendId: [team.backendId], // Null for new teams
          teamName: [team.teamName, Validators.required],
        }));
      });
    } else if (importMethod === 'append') {
      // Append new teams, avoiding duplicates
      teams.forEach((team) => {
        if (!this.teamsArray.value.some((existing: any) => existing.teamName === team.teamName)) {
          this.teamsArray.push(this.fb.group({
            frontendId: [team.frontendId],
            backendId: [team.backendId], 
            teamName: [team.teamName, Validators.required],
          }));
        }
      });
    }
  
    console.log('Updated Teams:', this.teamsArray.value);
  }
    
  handleMatchesExtracted(matches: Match[]): void {
    console.log('Matches Received from Child:', matches); // Log what the child emits
  
    const importMethod = this.tournamentForm.get('importMethod')?.value;
  
    if (importMethod === 'upload') {
      // Replace all matches
      this.matchesArray.clear();
      matches.forEach((match) => {
        console.log('Adding Match to FormArray:', match); // Log before pushing
        this.matchesArray.push(this.buildMatchFormGroup(match));
      });
    } else if (importMethod === 'append') {
      matches.forEach((match) => {
        if (
          !this.matchesArray.value.some(
            (existing: any) =>
              existing.frontendId === match.frontendId || 
              (existing.homeTeamFrontendId === match.homeTeamFrontendId &&
                existing.awayTeamFrontendId === match.awayTeamFrontendId &&
                existing.matchStart === match.matchStart)
          )
        ) {
          console.log('Appending Match to FormArray:', match); // Log before pushing
          this.matchesArray.push(this.buildMatchFormGroup(match));
        }
      });
    }
  
    console.log('Updated Matches (from FormArray.value):', this.matchesArray.value);
  }  
         
handleTeamsUpdated(teamsData: { previousTeams: Team[]; updatedTeams: Team[] }): void {
  const { previousTeams, updatedTeams } = teamsData;

  // Step 1: Create maps using frontendId for tracking changes
  const previousTeamMap = new Map(previousTeams.map(team => [team.frontendId, team]));
  const updatedTeamMap = new Map(updatedTeams.map(team => [team.frontendId, team]));

  // Step 2: Detect team name changes based on frontendId
  const nameUpdates = updatedTeams.filter(updatedTeam => {
    const previousTeam = previousTeamMap.get(updatedTeam.frontendId);
    return previousTeam && previousTeam.teamName !== updatedTeam.teamName;
  });

  // Step 3: Update matchesArray for team name changes
  if (nameUpdates.length > 0) {
    nameUpdates.forEach(updatedTeam => {
      this.matchesArray.controls.forEach((control: AbstractControl) => {
        const match = (control as FormGroup).value;

        if (match.homeTeamFrontendId === updatedTeam.frontendId) {
          (control as FormGroup).patchValue({ homeTeam: updatedTeam.teamName });
        }

        if (match.awayTeamFrontendId === updatedTeam.frontendId) {
          (control as FormGroup).patchValue({ awayTeam: updatedTeam.teamName });
        }
      });
    });
  }

  // Step 4: Remove matches where home or away teams no longer exist
  const updatedTeamFrontendIds = new Set(updatedTeams.map(team => team.frontendId));
  const filteredMatches = this.matchesArray.controls.filter((control: AbstractControl) => {
    const match = (control as FormGroup).value;
    return (
      updatedTeamFrontendIds.has(match.homeTeamFrontendId) &&
      updatedTeamFrontendIds.has(match.awayTeamFrontendId)
    );
  });

  // Step 5: Clear and rebuild matchesArray with filtered and updated matches
  this.matchesArray.clear();
  filteredMatches.forEach((control: AbstractControl) => {
    this.matchesArray.push(this.fb.group(control.value));
  });

  // Directly update matchesArray instead of emitting an event
  console.log('Updated Matches:', this.matchesArray.value);
}
   
  handleMatchesUpdated(updatedMatches: any[]): void {
    this.matchesArray.clear();
    updatedMatches.forEach(match => {
      this.matchesArray.push(buildMatchFormGroup(this.fb, match));
    });
    console.log('Updated Matches from Child:', this.matchesArray.value);
  }
  
  submitTournament(): void {
    // Validate Tournament Data
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
  
    // Show the spinner
    this.isLoading = true;
  
    // Prepare Tournament Data
    const isEditing = !!this.tournamentId;
  
    const tournamentData: Tournament = {
      tournamentId: isEditing ? this.tournamentId : null,
      tournamentName: this.tournamentForm.value.tournamentName,
      isActive: true,
      createdBy: this.tournamentForm.value.createdBy || 'Admin',
      createdAt: this.tournamentForm.value.createdAt || new Date().toISOString(),
      teams: this.teamsArray.value.map((team: { teamId: number | null; teamName: string }) => ({
        teamId: team.teamId || null, // Use `null` for new teams
        teamName: team.teamName,
      })),
      matches: this.matchesArray.value.map((match: any) => ({
        matchId: isEditing ? match.matchId || null : null,
        stage: match.stage || '',
        homeTeamId: isEditing ? match.homeTeamId || null : null,
        awayTeamId: isEditing ? match.awayTeamId || null : null,
        homeTeam: match.homeTeam,
        awayTeam: match.awayTeam,
        betType: match.betType || '90min',
        matchStart: new Date(match.matchStart).toISOString(),
        homeWinOdds: match.homeWinOdds,
        drawOdds: match.drawOdds,
        awayWinOdds: match.awayWinOdds,
        homeQualifies: match.homeQualifies,
        awayQualifies: match.awayQualifies,
      })),
    };
  
    console.log('Finalized Tournament Data:', tournamentData);
  
    const submitObservable = isEditing
      ? this.tournamentService.updatePredefinedTournament(tournamentData)
      : this.tournamentService.createPredefinedTournament(tournamentData);
  
    submitObservable.subscribe({
      next: (response) => {
        console.log('Server response:', response); // Log the plain text response
        this.router.navigate(['/tournaments/predefined']).then(() => {
          this.showToast('Tournament updated successfully!', 'success');
          this.isLoading = false; // Hide the spinner
        });
      },
      error: (error) => {
        console.error('Error submitting tournament:', error);
        this.showToast('Error submitting tournament!', 'danger');
        this.isLoading = false; // Hide the spinner even on error
      },
    });
  }
         
  async nextStep(): Promise<void> {
    const canProceed = await this.canProceed();
    if (canProceed && this.step < 4) {
      this.scrollToTop();
      this.step++;
    }
  }
  
  prevStep(): void {
    if (this.step > 1) {
      this.scrollToTop();
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
        return true; // No validation needed for the summary
    }
    return false; // Default fallback
  }
}
