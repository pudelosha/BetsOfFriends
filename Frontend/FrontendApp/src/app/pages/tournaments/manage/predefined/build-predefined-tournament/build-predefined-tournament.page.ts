import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, FormArray, FormControl, AbstractControl } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { IonicModule, ToastController, LoadingController } from '@ionic/angular';
import { StageInputTypePage } from '../../stages/stage-input-type/stage-input-type.page';
import { StageTeamsManagementPage } from '../../stages/stage-teams-management/stage-teams-management.page';
import { StageMatchesManagementPage } from '../../stages/stage-matches-management/stage-matches-management.page';
import { StageStagesManagementPage } from '../../stages/stage-stages-management/stage-stages-management.page';
import { StageSummaryPage } from '../../stages/stage-summary/stage-summary.page';
import { PredefinedTournamentService } from 'src/app/services/predefined-tournament.service';
import { Router, ActivatedRoute } from '@angular/router';
import { Tournament, Team, Match, Stage } from '../../../../../model/tournament-model';
import { ModalController } from '@ionic/angular';
import { ViewChild } from '@angular/core';

@Component({
  selector: 'app-build-predefined-tournament',
  templateUrl: './build-predefined-tournament.page.html',
  styleUrls: ['./build-predefined-tournament.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, StageInputTypePage, StageTeamsManagementPage, StageStagesManagementPage, StageMatchesManagementPage, StageSummaryPage],
})
export class BuildPredefinedTournamentPage implements OnInit {
  @ViewChild(StageTeamsManagementPage) stageTeamsManagement!: StageTeamsManagementPage;
  @ViewChild(StageMatchesManagementPage) stageMatchesManagement!: StageMatchesManagementPage;
  @ViewChild(StageStagesManagementPage) stageStagesManagement!: StageStagesManagementPage;

  tournamentForm: FormGroup;
  step = 1;
  tournamentId?: number | null = null; // Optional: null for new tournaments, number for existing ones
  isLoading = false;

  constructor(private fb: FormBuilder, 
    private toastController: ToastController,
    private router: Router,
    private route: ActivatedRoute,
    private tournamentService: PredefinedTournamentService,
    private modalController: ModalController,
    private loadingController: LoadingController
  ) {
    this.tournamentForm = this.fb.group({
      tournamentId: [null],
      tournamentName: ['', [Validators.required, Validators.maxLength(50)]],
      importMethod: ['upload'],
      teams: this.fb.array([], Validators.required),  // Holds Team models
      stages: this.fb.array([]),
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

  get stagesArray(): FormArray {
    return this.tournamentForm.get('stages') as FormArray;
  }

  async openAddModal(): Promise<void> {
    switch (this.step) {
      case 2:
        await this.stageTeamsManagement?.addTeam();
        break;
      case 3:
        await this.stageStagesManagement?.addStage();
        break;
      case 4:
        await this.stageMatchesManagement?.addMatch();
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
  
  private async populateForm(tournament: Tournament): Promise<void> {
    const loading = await this.loadingController.create({
      message: 'Loading tournament data...',
      spinner: 'crescent',
    });
    await loading.present();
  
    const startTime = Date.now(); // Track start time
  
    try {
      this.tournamentForm.patchValue({
        tournamentId: tournament.tournamentId,
        tournamentName: tournament.tournamentName,
        importMethod: 'upload',
      });
  
      // Step 1: Create a lookup map for teams (backendId -> frontendId & name)
      const teamMap = new Map<number, Team>();
  
      this.teamsArray.clear();
      tournament.teams.forEach((team) => {
        if (!team.teamFrontendId) {
          team.teamFrontendId = this.generateFrontendId(); // Ensure frontend ID exists
        }
  
        teamMap.set(team.teamId ?? 0, team); // Map backendId to team object
  
        this.teamsArray.push(
          this.fb.group({
            teamFrontendId: [team.teamFrontendId], // Ensure we store frontendId
            teamId: [team.teamId], // Backend ID
            teamName: [team.teamName, Validators.required],
          })
        );
      });
  
      // Step 2: Create a lookup map for stages (backendId -> frontendId & order)
      const stageMap = new Map<number, Stage>();
  
      this.stagesArray.clear();
      tournament.stages.forEach((stage) => {
        if (!stage.stageFrontendId) {
          stage.stageFrontendId = this.generateFrontendId(); // Ensure frontend ID exists
        }
  
        stageMap.set(stage.stageId ?? 0, stage); // Map backendId to stage object
  
        this.stagesArray.push(
          this.fb.group({
            stageFrontendId: [stage.stageFrontendId], // Ensure unique frontend ID
            stageId: [stage.stageId], // Backend ID
            stageName: [stage.stageName, Validators.required],
            order: [stage.order, [Validators.required, Validators.min(1)]],
          })
        );
      });
  
      // Step 3: Populate Matches and assign frontend IDs correctly
      this.matchesArray.clear();
      tournament.matches.forEach((match) => {
        const homeTeam = teamMap.get(match.homeTeamId ?? 0);
        const awayTeam = teamMap.get(match.awayTeamId ?? 0);
        const stage = stageMap.get(match.stageId ?? 0) || {
          stageFrontendId: this.generateFrontendId(),
          stageId: null,
          stageName: match.stageName || 'Default Stage'
        };
  
        this.matchesArray.push(
          this.fb.group({
            matchFrontendId: [match.matchFrontendId || this.generateFrontendId()], // Ensure unique frontendId
            matchId: [match.matchId], // Backend ID
  
            stageFrontendId: [stage.stageFrontendId], // Ensure frontend ID
            stageId: [stage.stageId], // Backend ID
            stageName: [stage.stageName], // Stage name
  
            homeTeamId: [match.homeTeamId], // Backend ID
            homeTeamFrontendId: [homeTeam?.teamFrontendId || this.generateFrontendId()], // Assign frontend ID
            homeTeam: [homeTeam?.teamName || match.homeTeam], // Ensure correct team name
  
            awayTeamId: [match.awayTeamId], // Backend ID
            awayTeamFrontendId: [awayTeam?.teamFrontendId || this.generateFrontendId()], // Assign frontend ID
            awayTeam: [awayTeam?.teamName || match.awayTeam], // Ensure correct team name
  
            matchStart: [new Date(match.matchStart).toISOString()],
            matchType: [match.matchType || 'Regular90Min'],
            homeWinOdds: [match.homeWinOdds],
            drawOdds: [match.drawOdds],
            awayWinOdds: [match.awayWinOdds],
            homeQualifies: [match.homeQualifies],
            awayQualifies: [match.awayQualifies],
          })
        );
      });
  
      console.log('Teams after population:', this.teamsArray.value);
      console.log('Matches after population:', this.matchesArray.value);
  
    } catch (error) {
      console.error('Error populating tournament form:', error);
    } finally {
      const elapsedTime = Date.now() - startTime;
      const delay = Math.max(0, 500 - elapsedTime);
  
      setTimeout(async () => {
        await loading.dismiss();
      }, delay);
    }
  }
    
  private buildMatchFormGroup(match: Match): FormGroup {
    return this.fb.group({
      matchFrontendId: [match.matchFrontendId],
      matchId: [match.matchId],
  
      stageFrontendId: [match.stageFrontendId],
      stageId: [match.stageId],
      stageName: [match.stageName],
  
      homeTeamId: [match.homeTeamId],
      homeTeamFrontendId: [match.homeTeamFrontendId],
      homeTeam: [match.homeTeam],
  
      awayTeamId: [match.awayTeamId],
      awayTeamFrontendId: [match.awayTeamFrontendId],
      awayTeam: [match.awayTeam],
  
      matchStart: [match.matchStart],
      matchType: [match.matchType || 'Regular90Min'],
      homeWinOdds: [match.homeWinOdds ?? 0],
      drawOdds: [match.drawOdds ?? 0],
      awayWinOdds: [match.awayWinOdds ?? 0],
      homeQualifies: [match.homeQualifies ?? null],
      awayQualifies: [match.awayQualifies ?? null],
    });
  }
            
  handleTeamsExtracted(teams: Team[]): void {
    const importMethod = this.tournamentForm.get('importMethod')?.value;
  
    console.log('Import Method:', importMethod);
  
    if (importMethod === 'upload') {
      // Replace all teams
      this.teamsArray.clear();
      teams.forEach((team) => {
        this.teamsArray.push(
          this.fb.group({
            teamFrontendId: [team.teamFrontendId || this.generateFrontendId()], // Ensure frontend ID
            teamId: [team.teamId], // Backend ID remains unchanged
            teamName: [team.teamName, Validators.required],
          })
        );
      });
    } else if (importMethod === 'append') {
      // Append new teams, avoiding duplicates
      teams.forEach((team) => {
        const existing = this.teamsArray.value.some(
          (existingTeam: any) => existingTeam.teamFrontendId === team.teamFrontendId
        );
  
        if (!existing) {
          this.teamsArray.push(
            this.fb.group({
              teamFrontendId: [team.teamFrontendId || this.generateFrontendId()], // Ensure frontend ID
              teamId: [team.teamId], // Backend ID remains unchanged
              teamName: [team.teamName, Validators.required],
            })
          );
        }
      });
    }
  
    console.log('Updated Teams:', this.teamsArray.value);
  }

  handleStagesExtracted(stages: Stage[]): void {
    const importMethod = this.tournamentForm.get('importMethod')?.value;
  
    console.log('Importing Stages - Method:', importMethod);
  
    if (importMethod === 'upload') {
      this.stagesArray.clear();
      stages.forEach(stage => {
        this.stagesArray.push(
          this.fb.group({
            stageFrontendId: [stage.stageFrontendId || this.generateFrontendId()],
            stageId: [stage.stageId],
            stageName: [stage.stageName, Validators.required],
            order: [stage.order, [Validators.required, Validators.min(1)]],
          })
        );
      });
    } else if (importMethod === 'append') {
      stages.forEach(stage => {
        const exists = this.stagesArray.value.some(
          (existingStage: any) => existingStage.stageFrontendId === stage.stageFrontendId
        );
  
        if (!exists) {
          this.stagesArray.push(
            this.fb.group({
              stageFrontendId: [stage.stageFrontendId || this.generateFrontendId()],
              stageId: [stage.stageId],
              stageName: [stage.stageName, Validators.required],
              order: [stage.order, [Validators.required, Validators.min(1)]],
            })
          );
        }
      });
    }
  
    console.log('Updated Stages:', this.stagesArray.value);
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
              existing.frontendId === match.matchFrontendId || 
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
  
    // Step 1: Create maps for easy lookup
    const previousTeamMap = new Map(previousTeams.map(team => [team.teamFrontendId, team])); // Map frontend ID to previous team
    const updatedTeamMap = new Map(updatedTeams.map(team => [team.teamFrontendId, team])); // Map frontend ID to updated team
  
    // Step 2: Detect team name changes based on frontendId
    const nameUpdates = updatedTeams.filter(updatedTeam => {
      const previousTeam = previousTeamMap.get(updatedTeam.teamFrontendId);
      return previousTeam && previousTeam.teamName !== updatedTeam.teamName;
    });
  
    // Step 3: Update matches where a team name has changed
    if (nameUpdates.length > 0) {
      nameUpdates.forEach(updatedTeam => {
        this.matchesArray.controls.forEach((control: AbstractControl) => {
          const match = (control as FormGroup).value;
  
          if (match.homeTeamFrontendId === updatedTeam.teamFrontendId) {
            (control as FormGroup).patchValue({ homeTeam: updatedTeam.teamName });
          }
          if (match.awayTeamFrontendId === updatedTeam.teamFrontendId) {
            (control as FormGroup).patchValue({ awayTeam: updatedTeam.teamName });
          }
        });
      });
    }
  
    // Step 4: Remove matches if a team no longer exists in the updated list
    const updatedTeamFrontendIds = new Set(updatedTeams.map(team => team.teamFrontendId));
    const filteredMatches = this.matchesArray.controls.filter((control: AbstractControl) => {
      const match = (control as FormGroup).value;
      return updatedTeamFrontendIds.has(match.homeTeamFrontendId) && updatedTeamFrontendIds.has(match.awayTeamFrontendId);
    });
  
    // Step 5: Clear and rebuild matchesArray with valid matches only
    this.matchesArray.clear();
    filteredMatches.forEach((control: AbstractControl) => {
      this.matchesArray.push(this.fb.group(control.value));
    });
  
    console.log('Updated Matches after team removal:', this.matchesArray.value);
  }

  handleStagesUpdated(stagesData: { previousStages: Stage[]; updatedStages: Stage[] }): void {
    const { previousStages, updatedStages } = stagesData;
    this.stagesArray.clear();
    updatedStages.forEach((stage) => {
      this.stagesArray.push(this.fb.group(stage));
    });
    console.log('Updated Stages:', this.stagesArray.value);
  }
     
  handleMatchesUpdated(updatedMatches: Match[]): void {
    this.matchesArray.clear();
    updatedMatches.forEach((match) => {
      this.matchesArray.push(this.buildMatchFormGroup(match));
    });
    console.log('Updated Matches from Child:', this.matchesArray.value);
  }  
  
  async submitTournament(): Promise<void> {
    // Validate Tournament Data
    if (!this.tournamentForm.value.tournamentName?.trim()) {
      this.showToast('Tournament name is required!', 'danger');
      return;
    }
  
    if (this.teamsArray.length < 2) {
      this.showToast('At least 2 teams are required to create a tournament!', 'danger');
      return;
    }
  
    if (this.stagesArray.length === 0) {
      this.showToast('At least one stage is required!', 'danger');
      return;
    }
  
    if (this.matchesArray.length < 1) {
      this.showToast('At least 1 match is required!', 'danger');
      return;
    }
  
    const loading = await this.loadingController.create({
      message: 'Submitting tournament...',
      spinner: 'crescent',
    });
    await loading.present();
  
    const startTime = Date.now(); // Track start time
  
    const isEditing = !!this.tournamentId;
  
    const tournamentData: Tournament = {
      tournamentId: isEditing ? this.tournamentId : null,
      tournamentName: this.tournamentForm.value.tournamentName,
      isActive: true,
      createdBy: this.tournamentForm.value.createdBy || 'Admin',
      createdAt: this.tournamentForm.value.createdAt || new Date().toISOString(),
      teams: this.teamsArray.value.map((team: { teamId: number | null; teamName: string }) => ({
        teamId: isEditing ? team.teamId || null : null,
        teamName: team.teamName,
      })),
      stages: this.stagesArray.value.map((stage: Stage) => ({
        stageId: isEditing ? stage.stageId || null : null,
        stageName: stage.stageName,
        order: stage.order,
      })),
      matches: this.matchesArray.value.map((match: any) => ({
        matchId: isEditing ? match.matchId || null : null,
        stageId: isEditing ? match.stageId || null : null,
        stageName: match.stageName,
        homeTeamId: isEditing ? match.homeTeamId || null : null,
        awayTeamId: isEditing ? match.awayTeamId || null : null,
        homeTeam: match.homeTeam,
        awayTeam: match.awayTeam,
        matchType: match.matchType || 'Regular90Min',
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
      next: async (response) => {
        console.log('Server response:', response);
        await this.router.navigate(['/tournaments/predefined']);
  
        this.showToast(
          isEditing ? 'Tournament updated successfully!' : 'Tournament created successfully!',
          'success'
        );
      },
      error: async (error) => {
        console.error('Error submitting tournament:', error);
        this.showToast('Error submitting tournament!', 'danger');
      },
      complete: async () => {
        const elapsedTime = Date.now() - startTime;
        const delay = Math.max(0, 500 - elapsedTime);
  
        setTimeout(async () => {
          await loading.dismiss();
        }, delay);
      },
    });
  }  
         
  async nextStep(): Promise<void> {
    const canProceed = await this.canProceed();
    if (canProceed && this.step < 5) {
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
        if (this.stagesArray.length === 0) {
          await this.showToast('At least 1 stage is required!', 'danger');
          return false;
        }
        return true;
  
      case 4:
        if (this.matchesArray.length === 0) {
          await this.showToast('At least 1 match is required!', 'danger');
          return false;
        }
        return true;
  
      case 5:
        return true; // No validation needed for the summary
    }
    return false; // Default fallback
  }

  private generateFrontendId(): string {
    return 'T-' + Math.random().toString(36).substr(2, 9);
  }
}
