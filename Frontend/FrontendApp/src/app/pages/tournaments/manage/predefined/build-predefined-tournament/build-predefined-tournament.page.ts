import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, FormArray, AbstractControl } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ToastController, LoadingController } from '@ionic/angular';
import { StageInputTypePage } from '../../stages/stage-input-type/stage-input-type.page';
import { StageTeamsManagementPage } from '../../stages/stage-teams-management/stage-teams-management.page';
import { StageMatchesManagementPage } from '../../stages/stage-matches-management/stage-matches-management.page';
import { StageStagesManagementPage } from '../../stages/stage-stages-management/stage-stages-management.page';
import { StageSummaryPage } from '../../stages/stage-summary/stage-summary.page';
import { PredefinedTournamentService } from 'src/app/services/predefined-tournament.service';
import { Router, ActivatedRoute } from '@angular/router';
import { Tournament, Team, Match, Stage } from '../../../../../model/tournament-model';
import { ViewChild } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { TitleService } from 'src/app/services/title.service';
import { IonContent, IonButton, IonSpinner } from '@ionic/angular/standalone';

@Component({
  selector: 'app-build-predefined-tournament',
  templateUrl: './build-predefined-tournament.page.html',
  styleUrls: ['./build-predefined-tournament.page.scss'],
  standalone: true,
  imports: [CommonModule, StageInputTypePage, StageTeamsManagementPage, StageStagesManagementPage, StageMatchesManagementPage, StageSummaryPage, TranslateModule, IonContent, IonButton, IonSpinner],
})
export class BuildPredefinedTournamentPage implements OnInit {
  @ViewChild(StageTeamsManagementPage) stageTeamsManagement!: StageTeamsManagementPage;
  @ViewChild(StageMatchesManagementPage) stageMatchesManagement!: StageMatchesManagementPage;
  @ViewChild(StageStagesManagementPage) stageStagesManagement!: StageStagesManagementPage;

  tournamentForm: FormGroup;
  step = 1;
  tournamentId?: number | null = null; // Optional: null for new tournaments, number for existing ones
  isLoading = false;
  isPredefinedTournament = true; // hardcoded since this is the "Predefined" builder

  constructor(private fb: FormBuilder, 
    private toastController: ToastController,
    private router: Router,
    private route: ActivatedRoute,
    private tournamentService: PredefinedTournamentService,
    private loadingController: LoadingController,
    private titleService: TitleService
  ) {
    this.tournamentForm = this.fb.group({
      tournamentId: [null],
      externalTournamentId: [null],
      tournamentName: ['', [Validators.required, Validators.maxLength(50)]],
      tournamentVisibility: ['Private', Validators.required], // or 'Public' depending on default
      updateMethod: ['Manual', Validators.required], 
      season: [null],
      seasonId: [null],
      tournamentStart: [null],
      tournamentEnd: [null],
      teams: this.fb.array([], Validators.required),  // Holds Team models
      stages: this.fb.array([]),
      matches: this.fb.array([]), // Holds Match models
    });
  }

  ngOnInit(): void {
    this.route.paramMap.subscribe((params) => {
      const id = params.get('id');
      if (id && !isNaN(+id)) {
        this.tournamentId = +id;
        this.titleService.setTitle('BUILD_PREDEFINED.EDIT_TITLE');
        this.loadTournament();
      } else {
        this.tournamentId = null;
        this.titleService.setTitle('BUILD_PREDEFINED.TITLE');
      }
    });
  }
  
  ionViewWillEnter(): void {
    const titleKey = this.isEditMode
      ? 'BUILD_PREDEFINED.EDIT_TITLE'
      : 'BUILD_PREDEFINED.TITLE';
    this.titleService.setTitle(titleKey);
  
    this.resetFormData();
    this.scrollToTop();
    this.step = 1;
  
    if (!this.isEditMode && this.isPredefinedTournament) {
      this.tournamentForm.patchValue({ updateMethod: 'Manual' });
    }
  
    if (!this.isEditMode && !this.isPredefinedTournament) {
      this.tournamentForm.patchValue({
        tournamentVisibility: 'Private',
        updateMethod: 'Manual',
      });
    }
  }
    
  get isEditMode(): boolean {
    return !!this.tournamentId;
  }

  private resetFormData(): void {
    this.tournamentForm.reset();
    this.teamsArray.clear();
    this.stagesArray.clear();
    this.matchesArray.clear();
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

  async loadTournament(): Promise<void> {
    if (!this.tournamentId) {
      console.error('Tournament ID is missing.');
      return;
    }

    const loading = await this.loadingController.create({
      message: 'Loading tournament...',
      spinner: 'crescent',
    });
    await loading.present();

    const startTime = Date.now();

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
      complete: async () => {
        const elapsedTime = Date.now() - startTime;
        const delay = Math.max(0, 500 - elapsedTime);
        setTimeout(() => loading.dismiss(), delay);
      }
    });
  } 
  
  private async populateForm(tournament: Tournament): Promise<void> {  
    try {
      this.tournamentForm.patchValue({
        tournamentId: tournament.tournamentId,
        externalTournamentId: tournament.externalTournamentId || null,
        tournamentName: tournament.tournamentName,
        tournamentVisibility: tournament.tournamentVisibility,
        updateMethod: tournament.updateMethod,
        season: tournament.season || null,
        seasonId: tournament.seasonId || null,
        tournamentStart: tournament.tournamentStart || null,
        tournamentEnd: tournament.tournamentEnd || null,
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
            externalTeamId: [team.externalTeamId || null],
            teamName: [team.teamName, Validators.required],
            recordStatus: ['Uploaded', Validators.required]
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
            recordStatus: ['Uploaded', Validators.required]
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
            externalMatchId: [match.externalMatchId || null],
  
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
            
            recordStatus: [
              match.matchStatus?.toLowerCase() === 'finished' ? 'Finalised' : 'Uploaded',
              Validators.required
            ],

            matchStatus: [match.matchStatus || null],    
            scoreHome: [match.scoreHome ?? null],       
            scoreAway: [match.scoreAway ?? null],        

            isVisible: [match.isVisible ?? true],
          })
        );
      });
  
      //console.log('Teams after population:', this.teamsArray.value);
      //console.log('Matches after population:', this.matchesArray.value);
  
    } catch (error) {
      console.error('Error populating tournament form:', error);
    }
  }
    
  private buildMatchFormGroup(match: Match): FormGroup {
    return this.fb.group({
      matchFrontendId: [match.matchFrontendId],
      matchId: [match.matchId],
      externalMatchId: [match.externalMatchId],
  
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

      isVisible: [match.isVisible ?? true],

      matchStatus: [match.matchStatus || null],
      scoreHome: [match.scoreHome ?? null],
      scoreAway: [match.scoreAway ?? null],

      recordStatus: [match.recordStatus ?? 'New'],
    });
  }
            
  handleTeamsExtracted(teams: Team[]): void {
    this.teamsArray.clear();
  
    teams.forEach((team) => {
      this.teamsArray.push(
        this.fb.group({
          teamFrontendId: [team.teamFrontendId || this.generateFrontendId()],
          teamId: [team.teamId],
          externalTeamId: [team.externalTeamId || null],
          teamName: [team.teamName, Validators.required],
          recordStatus: [team.recordStatus || 'New']
        })
      );
    });
  
    //console.log('Updated Teams:', this.teamsArray.value);
  }
  
  handleStagesExtracted(stages: Stage[]): void {
    this.stagesArray.clear();
  
    stages.forEach(stage => {
      this.stagesArray.push(
        this.fb.group({
          stageFrontendId: [stage.stageFrontendId || this.generateFrontendId()],
          stageId: [stage.stageId],
          stageName: [stage.stageName, Validators.required],
          order: [stage.order, [Validators.required, Validators.min(1)]],
          recordStatus: [stage.recordStatus || 'New']
        })
      );
    });
  
    //console.log('Updated Stages:', this.stagesArray.value);
  }   
    
  handleMatchesExtracted(matches: Match[]): void {
    //console.log('Matches Received from Child:', matches);
  
    this.matchesArray.clear();
  
    matches.forEach((match) => {
      this.matchesArray.push(this.buildMatchFormGroup({
        ...match,
        recordStatus: match.recordStatus || 'New'
      }));
    });
  
    //console.log('Updated Matches (from FormArray.value):', this.matchesArray.value);
  }
            
  handleTeamsUpdated(teamsData: { previousTeams: Team[]; updatedTeams: Team[] }): void {
    const { previousTeams, updatedTeams } = teamsData;
  
    // Step 1: Create lookup maps for previous and updated teams
    const previousTeamMap = new Map(previousTeams.map(team => [team.teamFrontendId, team]));
    const updatedTeamMap = new Map(updatedTeams.map(team => [team.teamFrontendId, team]));
  
    // Step 2: Identify removed teams and mark them as "Delete"
    previousTeams.forEach(team => {
      if (!updatedTeamMap.has(team.teamFrontendId)) {
        updatedTeams.push({ ...team, recordStatus: 'Delete' });
      }
    });
  
    // Step 3: Update teamsArray and handle name updates
    this.teamsArray.clear();
    updatedTeams.forEach(team => {
      const previousTeam = previousTeamMap.get(team.teamFrontendId);
      const isUpdated = previousTeam && previousTeam.teamName !== team.teamName;
  
      this.teamsArray.push(
        this.fb.group({
          teamFrontendId: [team.teamFrontendId],
          teamId: [team.teamId],
          teamName: [team.teamName, Validators.required],
          recordStatus: [isUpdated ? 'Update' : team.recordStatus || 'New']
        })
      );
  
      // If the team name changed, update matches
      if (isUpdated) {
        this.matchesArray.controls.forEach((control: AbstractControl) => {
          const match = (control as FormGroup).value;
          if (match.homeTeamFrontendId === team.teamFrontendId) {
            (control as FormGroup).patchValue({ homeTeam: team.teamName });
          }
          if (match.awayTeamFrontendId === team.teamFrontendId) {
            (control as FormGroup).patchValue({ awayTeam: team.teamName });
          }
        });
      }
    });
  
    // Step 4: Handle match deletions and status updates
    const validTeamFrontendIds = new Set(updatedTeams.map(team => team.teamFrontendId));
    const matchesToKeep: AbstractControl[] = [];
  
    this.matchesArray.controls.forEach((control: AbstractControl) => {
      const match = (control as FormGroup).value;
  
      const homeTeam = updatedTeamMap.get(match.homeTeamFrontendId);
      const awayTeam = updatedTeamMap.get(match.awayTeamFrontendId);
  
      if (!validTeamFrontendIds.has(match.homeTeamFrontendId) || !validTeamFrontendIds.has(match.awayTeamFrontendId)) {
        // If a team was deleted from the list (not marked as "Delete"), DELETE its matches
        //console.log(`Deleting match: ${match.homeTeam} vs ${match.awayTeam}`);
      } else if (homeTeam?.recordStatus === 'Delete' || awayTeam?.recordStatus === 'Delete') {
        // If a team is marked "Delete", mark its matches as "Delete"
        //console.log(`Marking match as 'Delete': ${match.homeTeam} vs ${match.awayTeam}`);
        (control as FormGroup).patchValue({ recordStatus: 'Delete' });
        matchesToKeep.push(control);
      } else if (previousTeamMap.get(match.homeTeamFrontendId)?.recordStatus === 'Delete' ||
                 previousTeamMap.get(match.awayTeamFrontendId)?.recordStatus === 'Delete') {
        // If a previously deleted team was restored, mark its matches as "Update"
        //console.log(`Restoring match: ${match.homeTeam} vs ${match.awayTeam}`);
        (control as FormGroup).patchValue({ recordStatus: 'Update' });
        matchesToKeep.push(control);
      } else {
        matchesToKeep.push(control);
      }
    });
  
    // Step 5: Rebuild matchesArray with only valid matches
    this.matchesArray.clear();
    matchesToKeep.forEach(control => this.matchesArray.push(control));
  
    //console.log('Updated Matches after team removal or undo:', this.matchesArray.value);
  }
      
  handleStagesUpdated(stagesData: { previousStages: Stage[]; updatedStages: Stage[] }): void {
    const { previousStages, updatedStages } = stagesData;
  
    // Step 1: Create lookup maps for previous and updated stages
    const previousStageMap = new Map(previousStages.map(stage => [stage.stageFrontendId, stage]));
    const updatedStageMap = new Map(updatedStages.map(stage => [stage.stageFrontendId, stage]));
  
    // Step 2: Identify removed stages and mark them as "Delete"
    previousStages.forEach(stage => {
      if (!updatedStageMap.has(stage.stageFrontendId)) {
        updatedStages.push({ ...stage, recordStatus: 'Delete' });
      }
    });
  
    // Step 3: Update stagesArray and detect changes
    this.stagesArray.clear();
    updatedStages.forEach(stage => {
      const previousStage = previousStageMap.get(stage.stageFrontendId);
      const isUpdated = previousStage &&
        (previousStage.stageName !== stage.stageName || previousStage.order !== stage.order);
  
      this.stagesArray.push(
        this.fb.group({
          stageFrontendId: [stage.stageFrontendId],
          stageId: [stage.stageId],
          stageName: [stage.stageName, Validators.required],
          order: [stage.order, [Validators.required, Validators.min(1)]],
          recordStatus: [isUpdated ? 'Update' : stage.recordStatus || 'New']
        })
      );
  
      // If the stage name changed, update matches
      if (isUpdated) {
        this.matchesArray.controls.forEach((control: AbstractControl) => {
          const match = (control as FormGroup).value;
          if (match.stageFrontendId === stage.stageFrontendId) {
            (control as FormGroup).patchValue({ stageName: stage.stageName });
          }
        });
      }
    });
  
    // Step 4: Handle match deletions and status updates
    const validStageFrontendIds = new Set(updatedStages.map(stage => stage.stageFrontendId));
    const matchesToKeep: AbstractControl[] = [];
  
    this.matchesArray.controls.forEach((control: AbstractControl) => {
      const match = (control as FormGroup).value;
      const stage = updatedStageMap.get(match.stageFrontendId);
  
      if (!validStageFrontendIds.has(match.stageFrontendId)) {
        // If a stage was deleted from the list (not marked as "Delete"), DELETE its matches
        //console.log(`Deleting match due to removed stage: ${match.homeTeam} vs ${match.awayTeam}`);
      } else if (stage?.recordStatus === 'Delete') {
        // If a stage is marked "Delete", mark its matches as "Delete"
        //console.log(`Marking match as 'Delete' due to stage deletion: ${match.homeTeam} vs ${match.awayTeam}`);
        (control as FormGroup).patchValue({ recordStatus: 'Delete' });
        matchesToKeep.push(control);
      } else if (previousStageMap.get(match.stageFrontendId)?.recordStatus === 'Delete') {
        // If a previously deleted stage was restored, mark its matches as "Update"
        //console.log(`Restoring match due to restored stage: ${match.homeTeam} vs ${match.awayTeam}`);
        (control as FormGroup).patchValue({ recordStatus: 'Update' });
        matchesToKeep.push(control);
      } else {
        matchesToKeep.push(control);
      }
    });
  
    // Step 5: Rebuild matchesArray with only valid matches
    this.matchesArray.clear();
    matchesToKeep.forEach(control => this.matchesArray.push(control));
  
    //console.log('Updated Matches after stage removal or undo:', this.matchesArray.value);
  }  
       
  handleMatchesUpdated(updatedMatches: Match[]): void {
    const previousMatches: Match[] = this.matchesArray.value; // Previous state
    const previousMatchMap = new Map(previousMatches.map(match => [match.matchFrontendId, match])); // Fast lookup
  
    // Step 1: Identify deleted matches
    previousMatches.forEach(match => {
      if (!updatedMatches.some(updated => updated.matchFrontendId === match.matchFrontendId)) {
        updatedMatches.push({ ...match, recordStatus: 'Delete' });
      }
    });
  
    // Step 2: Detect updates & rebuild matchesArray
    this.matchesArray.clear();
    updatedMatches.forEach(match => {
      const previousMatch = previousMatchMap.get(match.matchFrontendId);
      const isUpdated = previousMatch && (
        previousMatch.stageFrontendId !== match.stageFrontendId ||
        previousMatch.homeTeamFrontendId !== match.homeTeamFrontendId ||
        previousMatch.awayTeamFrontendId !== match.awayTeamFrontendId ||
        previousMatch.matchStart !== match.matchStart ||
        previousMatch.matchType !== match.matchType ||
        previousMatch.homeWinOdds !== match.homeWinOdds ||
        previousMatch.drawOdds !== match.drawOdds ||
        previousMatch.awayWinOdds !== match.awayWinOdds ||
        previousMatch.homeQualifies !== match.homeQualifies ||
        previousMatch.awayQualifies !== match.awayQualifies ||
        previousMatch.matchStatus !== match.matchStatus ||             
        previousMatch.scoreHome !== match.scoreHome ||              
        previousMatch.scoreAway !== match.scoreAway             
      );
  
      this.matchesArray.push(this.fb.group({
        matchFrontendId: [match.matchFrontendId],
        matchId: [match.matchId],
        externalMatchId: [match.externalMatchId],
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
        isVisible: [match.isVisible ?? true],
        matchStatus: [match.matchStatus || null],
        scoreHome: [match.scoreHome ?? null],
        scoreAway: [match.scoreAway ?? null],
        recordStatus: [isUpdated ? 'Update' : match.recordStatus || 'New']
      }));
    });
  
    // Step 3: Handle match restoration (UNDO)
    updatedMatches.forEach(match => {
      if (match.recordStatus === 'Update') {
        // Check if corresponding teams and stage were "Deleted" and restore them
        this.teamsArray.controls.forEach(control => {
          const team = control.value;
          if ((team.teamFrontendId === match.homeTeamFrontendId || team.teamFrontendId === match.awayTeamFrontendId)
            && team.recordStatus === 'Delete') {
            //console.log(`Restoring team: ${team.teamName}`);
            (control as FormGroup).patchValue({ recordStatus: 'Update' });
          }
        });
  
        this.stagesArray.controls.forEach(control => {
          const stage = control.value;
          if (stage.stageFrontendId === match.stageFrontendId && stage.recordStatus === 'Delete') {
            //console.log(`Restoring stage: ${stage.stageName}`);
            (control as FormGroup).patchValue({ recordStatus: 'Update' });
          }
        });
      }
    });
  
    //console.log('Updated Matches:', this.matchesArray.value);
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
      externalTournamentId: this.tournamentForm.value.externalTournamentId || null,
      season: this.tournamentForm.value.season || null,
      seasonId: this.tournamentForm.value.seasonId || null,
      tournamentStart: this.tournamentForm.value.tournamentStart || null,
      tournamentEnd: this.tournamentForm.value.tournamentEnd || null,
      tournamentName: this.tournamentForm.value.tournamentName,
      tournamentVisibility: this.tournamentForm.value.tournamentVisibility,
      updateMethod: this.tournamentForm.value.updateMethod,
      isActive: true,
      createdBy: this.tournamentForm.value.createdBy || 'Admin',
      createdAt: this.tournamentForm.value.createdAt || new Date().toISOString(),

      teams: this.teamsArray.value.map((team: Team) => ({
        teamId: isEditing ? team.teamId || null : null,
        externalTeamId: team.externalTeamId || null,
        teamName: team.teamName,
        recordStatus: team.recordStatus || 'New'
      })),

      stages: this.stagesArray.value.map((stage: Stage) => ({
        stageId: isEditing ? stage.stageId || null : null,
        stageName: stage.stageName,
        order: stage.order,
        recordStatus: stage.recordStatus || 'New'
      })),
      
      matches: this.matchesArray.value.map((match: any) => ({
        matchId: isEditing ? match.matchId || null : null,
        externalMatchId: match.externalMatchId || null,
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
        matchStatus: match.matchStatus || null,   
        scoreHome: match.scoreHome ?? null,        
        scoreAway: match.scoreAway ?? null,     
        isVisible: match.isVisible ?? true,
        recordStatus: match.recordStatus || 'New'
      })),
    };
  
    //console.log('Finalized Tournament Data:', tournamentData);
  
    const submitObservable = isEditing
      ? this.tournamentService.updatePredefinedTournament(tournamentData)
      : this.tournamentService.createPredefinedTournament(tournamentData);
  
    submitObservable.subscribe({
      next: async (response) => {
        //console.log('Server response:', response);
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
