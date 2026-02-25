import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, FormArray, AbstractControl } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ToastController, LoadingController  } from '@ionic/angular';
import { StageInputTypePage } from '../../stages/stage-input-type/stage-input-type.page';
import { StageTeamsManagementPage } from '../../stages/stage-teams-management/stage-teams-management.page';
import { StageStagesManagementPage } from '../../stages/stage-stages-management/stage-stages-management.page';
import { StageMatchesManagementPage } from '../../stages/stage-matches-management/stage-matches-management.page';
import { StageUsersManagementPage } from '../../stages/stage-users-management/stage-users-management.page';
import { StageSummaryPage } from '../../stages/stage-summary/stage-summary.page';
import { StageSettingsPage } from '../../stages/stage-settings/stage-settings.page';
import { Router, ActivatedRoute } from '@angular/router';
import { Tournament, TournamentSettings } from 'src/app/model/tournament-model';
import { CustomTournamentService } from 'src/app/services/custom-tournament.service';
import { ViewChild } from '@angular/core';
import { Match, Team, User, Stage, RecordStatus } from 'src/app/model/tournament-model';
import { firstValueFrom } from 'rxjs/internal/firstValueFrom';
import { TournamentSelectionService } from 'src/app/services/tournament-selection.service';
import { TranslateModule } from '@ngx-translate/core';
import { TitleService } from 'src/app/services/title.service';
import { IonContent, IonButton, IonSpinner } from '@ionic/angular/standalone';

@Component({
  selector: 'app-build-custom-tournament',
  templateUrl: './build-custom-tournament.page.html',
  styleUrls: ['./build-custom-tournament.page.scss'],
  standalone: true,
  imports: [CommonModule, StageInputTypePage, StageTeamsManagementPage, StageStagesManagementPage, StageMatchesManagementPage, StageUsersManagementPage, StageSummaryPage, StageSettingsPage, TranslateModule, IonContent, IonButton, IonSpinner],
})
export class BuildCustomTournamentPage implements OnInit {
  @ViewChild(StageInputTypePage) stageInputTypePage!: StageInputTypePage;
  @ViewChild(StageTeamsManagementPage) stageTeamsManagement!: StageTeamsManagementPage;
  @ViewChild(StageStagesManagementPage) stageStagesManagement!: StageStagesManagementPage;
  @ViewChild(StageMatchesManagementPage) stageMatchesManagement!: StageMatchesManagementPage;
  @ViewChild(StageUsersManagementPage) stageUsersManagement!: StageUsersManagementPage;

  tournamentForm: FormGroup;
  step = 1;
  tournamentId?: number | null = null;
  isLoading = false;
  settingsConfirmed = false;
  isPredefinedTournament = false;

  constructor(private fb: FormBuilder, 
    private toastController: ToastController,
    private router: Router,
    private route: ActivatedRoute,
    private tournamentService: CustomTournamentService,
    private loadingController: LoadingController,
    private tournamentSelectionService: TournamentSelectionService,
    private titleService: TitleService
  ) {
    this.tournamentForm = this.fb.group({
      tournamentId: [null],
      externalTournamentId: [null],
      predefinedTournamentId: [null],
      tournamentName: ['', [Validators.required, Validators.maxLength(50)]],
      publicTournamentName: [null],
      tournamentVisibility: ['Private', Validators.required],
      updateMethod: ['Manual', Validators.required], 
      season: [null],              
      tournamentEnd: [null],      
      teams: this.fb.array([], Validators.required),
      stages: this.fb.array([], Validators.required),
      matches: this.fb.array([], Validators.required),
      users: this.fb.array([]),
      settings: this.fb.group({
        allowExactResultBonus: [false],
        exactResultBonusCalculation: ['Fixed'],
        exactResultBonus: [null, Validators.min(1)],
        allowWhoQualifiesBets: [false],
        allowBetsWithBooster: [false],
        maxBetBooster: [1, Validators.min(1)],
        totalBoosterPool: [null, Validators.min(1)],
        allowNonSubmittedBetsPenalty: [false],
        nonSubmittedBetPenalty: [null, Validators.min(1)],
      }),
    });
  }

  ngOnInit(): void {
    this.route.paramMap.subscribe((params) => {
      const id = params.get('id');
      this.tournamentId = id && !isNaN(+id) ? +id : null;
  
      const titleKey = this.isEditMode
        ? 'BUILD_CUSTOM.EDIT_TITLE'
        : 'BUILD_CUSTOM.TITLE';
      this.titleService.setTitle(titleKey);
  
      if (this.isEditMode) {
        this.loadTournament();
      }
    });
  }

  ionViewWillEnter(): void {
    const titleKey = this.isEditMode
      ? 'BUILD_CUSTOM.EDIT_TITLE'
      : 'BUILD_CUSTOM.TITLE';
    this.titleService.setTitle(titleKey);
  
    this.scrollToTop();
    this.step = 1;
  
    if (!this.isEditMode && !this.isPredefinedTournament) {
      this.resetFormData();
  
      this.tournamentForm.patchValue({
        tournamentVisibility: 'Private',
        updateMethod: 'Manual',
      });
  
      setTimeout(() => {
        this.stageInputTypePage?.loadPredefinedTournaments?.();
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
    this.usersArray.clear();
  }

  private scrollToTop(): void {
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  get teamsArray(): FormArray {
    return this.tournamentForm.get('teams') as FormArray;
  }

  get stagesArray(): FormArray {
    return this.tournamentForm.get('stages') as FormArray;
  }

  get matchesArray(): FormArray {
    return this.tournamentForm.get('matches') as FormArray;
  }

  get usersArray(): FormArray {
    return this.tournamentForm.get('users') as FormArray;
  }

  get settingsGroup(): FormGroup {
    return this.tournamentForm.get('settings') as FormGroup;
  }

  async openAddModal(): Promise<void> {
    switch (this.step) {
      case 3:
        await this.stageTeamsManagement?.addTeam();
        break;
      case 4:
        await this.stageStagesManagement?.addStage();
        break;
      case 5:
        await this.stageMatchesManagement?.addMatch();
        break;
      case 6:
        await this.stageUsersManagement?.addUser();
        break;
    }
  }
      
  async loadTournament(): Promise<void> {
    if (!this.tournamentId) {
      return;
    }

    const loading = await this.loadingController.create({
      message: 'Loading tournament...',
      spinner: 'crescent',
    });
    await loading.present();

    const startTime = Date.now();

    this.tournamentService.getCustomTournamentById(this.tournamentId).subscribe({
      next: (tournament) => {
        if (tournament) {
          this.populateForm(tournament);
        } else {
        }
      },
      error: (err) => {
      },
      complete: async () => {
        const elapsedTime = Date.now() - startTime;
        const delay = Math.max(0, 500 - elapsedTime);
        setTimeout(() => loading.dismiss(), delay);
      }
    });
  }
   
  populateForm(tournament: Tournament): void {  
    this.tournamentForm.patchValue({
      tournamentId: tournament.tournamentId,
      externalTournamentId: tournament.externalTournamentId || null,
      predefinedTournamentId: tournament.predefinedTournamentId || null,
      tournamentName: tournament.tournamentName,
      tournamentVisibility: tournament.tournamentVisibility || 'Private',
      publicTournamentName: tournament.publicTournamentName || '',
      updateMethod: tournament.updateMethod || 'Manual',
      season: tournament.season || null,                   
      tournamentEnd: tournament.tournamentEnd || null        
    });

    if (tournament.settings) {
      this.settingsGroup.patchValue({
        allowExactResultBonus: tournament.settings.allowExactResultBonus ?? false,
        exactResultBonusCalculation: tournament.settings.exactResultBonusCalculation ?? 'Fixed',
        exactResultBonus: tournament.settings.exactResultBonus ?? null,
        allowWhoQualifiesBets: tournament.settings.allowWhoQualifiesBets ?? false,
        allowBetsWithBooster: tournament.settings.allowBetsWithBooster ?? false,
        maxBetBooster: tournament.settings.maxBetBooster ?? 1,
        totalBoosterPool: tournament.settings.totalBoosterPool ?? null,
        allowNonSubmittedBetsPenalty: tournament.settings.allowNonSubmittedBetsPenalty ?? false,
        nonSubmittedBetPenalty: tournament.settings.nonSubmittedBetPenalty ?? null,
      });
    }
  
    // Step 1: Create a lookup map for teams (backendId -> frontendId & name)
    const teamMap = new Map<number, Team>();
  
    this.teamsArray.clear();
    tournament.teams.forEach((team) => {
      if (!team.teamFrontendId) {
        team.teamFrontendId = this.generateFrontendId();
      }
      
      teamMap.set(team.teamId ?? 0, team);
  
      this.teamsArray.push(
        this.fb.group({
          teamFrontendId: [team.teamFrontendId],
          externalTeamId: [team.externalTeamId || null],
          teamId: [team.teamId],
          predefinedTeamId: [team.predefinedTeamId || null],
          teamName: [team.teamName, Validators.required],
          eloRating: [
            team.eloRating ?? 1000,
            [Validators.required, Validators.min(0), Validators.max(5000)],
          ],
          recordStatus: ['Uploaded', Validators.required]
        })
      );
    });

    // Step 2: Create a lookup map for stages (backendId -> frontendId & order)
    const stageMap = new Map<number, Stage>();

    this.stagesArray.clear();
    tournament.stages.forEach((stage) => {
      if (!stage.stageFrontendId) {
        stage.stageFrontendId = this.generateFrontendId();
      }

      stageMap.set(stage.stageId ?? 0, stage);

      this.stagesArray.push(
        this.fb.group({
          stageFrontendId: [stage.stageFrontendId],
          stageId: [stage.stageId],
          predefinedStageId: [stage.predefinedStageId || null],
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
          matchFrontendId: [match.matchFrontendId || this.generateFrontendId()],
          matchId: [match.matchId],
          externalMatchId: [match.externalMatchId || null],
          predefinedMatchId: [match.predefinedMatchId || null],

          stageFrontendId: [stage.stageFrontendId || this.generateFrontendId()],
          stageId: [stage.stageId],
          stageName: [stage.stageName],
  
          homeTeamId: [match.homeTeamId],
          homeTeamFrontendId: [homeTeam?.teamFrontendId || this.generateFrontendId()],
          homeTeam: [homeTeam?.teamName || match.homeTeam],
  
          awayTeamId: [match.awayTeamId],
          awayTeamFrontendId: [awayTeam?.teamFrontendId || this.generateFrontendId()],
          awayTeam: [awayTeam?.teamName || match.awayTeam],
  
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
          qualifiedTeam: [match.qualifiedTeam ?? null],

          isVisible: [match.isVisible ?? true],
        })
      );
    });

    // Step 3: Populate Users (With Safe Check)
    this.usersArray.clear();
    if (tournament.users?.length) {
      tournament.users.forEach((user) => {
        this.usersArray.push(
          this.fb.group({
            assignmentId: [user.assignmentId || null],
            userAdminName: [user.userAdminName || '', Validators.required],
            userEmail: [user.userEmail || '', [Validators.required, Validators.email]],
            status: [user.status || 'New', Validators.required],
            userRole: [user.userRole || 'Player', Validators.required],
            recordStatus: ['Uploaded', Validators.required]
          })
        );
      });
    }
  } 

  private buildMatchFormGroup(match: Match): FormGroup {
    return this.fb.group({
      matchFrontendId: [match.matchFrontendId],
      matchId: [match.matchId],
      externalMatchId: [match.externalMatchId || null],
      predefinedMatchId: [match.predefinedMatchId || null],
      stageFrontendId: [match.stageFrontendId],
      stageId: [match.stageId],
      stageName: [match.stageName],
      homeTeamId: [match.homeTeamId],
      homeTeamFrontendId: [match.homeTeamFrontendId],
      homeTeam: [match.homeTeam],
      awayTeamId: [match.awayTeamId],
      awayTeamFrontendId: [match.awayTeamFrontendId],
      awayTeam: [match.awayTeam],
      matchStart: [new Date(match.matchStart).toISOString()],
      matchType: [match.matchType || 'Regular90Min'],
      homeWinOdds: [match.homeWinOdds],
      drawOdds: [match.drawOdds],
      awayWinOdds: [match.awayWinOdds],
      homeQualifies: [match.homeQualifies],
      awayQualifies: [match.awayQualifies],
      recordStatus: [match.recordStatus ?? 'New'],
      isVisible: [match.isVisible ?? true],
      matchStatus: [match.matchStatus || null],
      scoreHome: [match.scoreHome ?? null],
      scoreAway: [match.scoreAway ?? null],
      qualifiedTeam: [match.qualifiedTeam ?? null]
    });
  }
         
  handleTeamsExtracted(teams: Team[]): void {
    this.teamsArray.clear();
  
    teams.forEach((team) => {
      this.teamsArray.push(
        this.fb.group({
          teamFrontendId: [team.teamFrontendId || this.generateFrontendId()],
          externalTeamId: [team.externalTeamId || null],
          teamId: [team.teamId],
          predefinedTeamId: [team.predefinedTeamId || null],
          teamName: [team.teamName, Validators.required],
          eloRating: [
            team.eloRating ?? 1000,
            [Validators.required, Validators.min(0), Validators.max(5000)],
          ],
          recordStatus: [team.recordStatus || 'New']
        })
      );
    });  
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
  }   
    
  handleMatchesExtracted(matches: Match[]): void {
    this.matchesArray.clear();
  
    matches.forEach((match) => {
      this.matchesArray.push(this.buildMatchFormGroup({
        ...match,
        recordStatus: match.recordStatus || 'New'
      }));
    });
  }
  
  handleUsersUpdated(users: User[]): void {  
    const previousUsers: User[] = this.usersArray.value;
    const previousUserMap = new Map(previousUsers.map(user => [user.assignmentId, user])); 
  
    this.usersArray.clear();
    users.forEach(user => {
      const previousUser = previousUserMap.get(user.assignmentId);
      let recordStatus: RecordStatus = user.recordStatus || 'New';
  
      if (previousUser) {
        if (previousUser.recordStatus === 'Delete' && recordStatus !== 'Delete') {
          recordStatus = 'Update';
        }
      }
  
      if (recordStatus === 'Delete' && user.recordStatus !== 'New') {
      } else if (recordStatus === 'Delete') {
        return;
      }
  
      this.usersArray.push(
        this.fb.group({
          assignmentId: [user.assignmentId || null],
          userName: [user.userName, Validators.required],
          userAdminName: [user.userAdminName],
          userEmail: [user.userEmail, [Validators.required, Validators.email]],
          status: [user.status, Validators.required],
          userRole: [user.userRole, Validators.required],
          recordStatus: [recordStatus]
        })
      );
    });
  }   

  handleSettingsUpdated(updatedSettings: TournamentSettings): void {
    this.settingsGroup.patchValue(updatedSettings);
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
      const isUpdated = previousTeam &&
        (previousTeam.teamName !== team.teamName ||
         Number(previousTeam.eloRating ?? 1000) !== Number(team.eloRating ?? 1000));
  
      this.teamsArray.push(
        this.fb.group({
          teamFrontendId: [team.teamFrontendId],
          teamId: [team.teamId],
          teamName: [team.teamName, Validators.required],
          eloRating: [
            Number(team.eloRating ?? 1000),
            [Validators.required, Validators.min(0), Validators.max(5000)],
          ],
          recordStatus: [isUpdated ? 'Update' : team.recordStatus || 'New']
        })
      );
  
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
        //console.log(`Deleting match: ${match.homeTeam} vs ${match.awayTeam}`);
      } else if (homeTeam?.recordStatus === 'Delete' || awayTeam?.recordStatus === 'Delete') {
        //console.log(`Marking match as 'Delete': ${match.homeTeam} vs ${match.awayTeam}`);
        (control as FormGroup).patchValue({ recordStatus: 'Delete' });
        matchesToKeep.push(control);
      } else if (previousTeamMap.get(match.homeTeamFrontendId)?.recordStatus === 'Delete' ||
                 previousTeamMap.get(match.awayTeamFrontendId)?.recordStatus === 'Delete') {
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
        //console.log(`Deleting match due to removed stage: ${match.homeTeam} vs ${match.awayTeam}`);
      } else if (stage?.recordStatus === 'Delete') {
        //console.log(`Marking match as 'Delete' due to stage deletion: ${match.homeTeam} vs ${match.awayTeam}`);
        (control as FormGroup).patchValue({ recordStatus: 'Delete' });
        matchesToKeep.push(control);
      } else if (previousStageMap.get(match.stageFrontendId)?.recordStatus === 'Delete') {
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
  }  
       
  handleMatchesUpdated(updatedMatches: Match[]): void {
    const previousMatches: Match[] = this.matchesArray.value;
    const previousMatchMap = new Map(previousMatches.map(match => [match.matchFrontendId, match]));
  
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
        previousMatch.awayQualifies !== match.awayQualifies
      );
  
      this.matchesArray.push(this.fb.group({
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
        matchStatus: [match.matchStatus || null],
        scoreHome: [match.scoreHome ?? null],
        scoreAway: [match.scoreAway ?? null],
        qualifiedTeam: [match.qualifiedTeam ?? null],
        recordStatus: [isUpdated ? 'Update' : match.recordStatus || 'New'],
        isVisible: [match.isVisible ?? true],
      }));
    });
  
    // Step 3: Handle match restoration (UNDO)
    updatedMatches.forEach(match => {
      if (match.recordStatus === 'Update') {
        this.teamsArray.controls.forEach(control => {
          const team = control.value;
          if ((team.teamFrontendId === match.homeTeamFrontendId || team.teamFrontendId === match.awayTeamFrontendId)
            && team.recordStatus === 'Delete') {
            (control as FormGroup).patchValue({ recordStatus: 'Update' });
          }
        });
  
        this.stagesArray.controls.forEach(control => {
          const stage = control.value;
          if (stage.stageFrontendId === match.stageFrontendId && stage.recordStatus === 'Delete') {
            (control as FormGroup).patchValue({ recordStatus: 'Update' });
          }
        });
      }
    });
  } 

  async submitTournament(): Promise<void> {
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
  
    const startTime = Date.now();
  
    const isEditing = !!this.tournamentId;
  
    const tournamentData: Tournament = {
      tournamentId: isEditing ? this.tournamentId : null,
      predefinedTournamentId: this.tournamentForm.value.predefinedTournamentId || null,
      externalTournamentId: this.tournamentForm.value.externalTournamentId || null,
      tournamentName: this.tournamentForm.value.tournamentName,
      publicTournamentName: this.tournamentForm.value.publicTournamentName?.trim() || undefined,
      tournamentVisibility: this.tournamentForm.value.tournamentVisibility || 'Private',
      updateMethod: this.tournamentForm.value.updateMethod || 'Manual',
      season: this.tournamentForm.value.season || null,
      tournamentEnd: this.tournamentForm.value.tournamentEnd || null,
      isActive: true,
      createdBy: this.tournamentForm.value.createdBy || 'Admin',
      createdAt: this.tournamentForm.value.createdAt || new Date().toISOString(),

      teams: this.teamsArray.value.map((team: Team) => ({
        teamId: isEditing ? team.teamId || null : null,
        externalTeamId: team.externalTeamId || null,
        predefinedTeamId: team.predefinedTeamId || null,
        teamName: team.teamName,
        eloRating: Number(team.eloRating ?? 1000),
        recordStatus: team.recordStatus || 'New'
      })),

      stages: this.stagesArray.value.map((stage: Stage) => ({
        stageId: isEditing ? stage.stageId || null : null,
        predefinedStageId: stage.predefinedStageId || null,
        stageName: stage.stageName,
        order: stage.order,
        recordStatus: stage.recordStatus || 'New'
      })),

      matches: this.matchesArray.value.map((match: any) => ({
        matchId: isEditing ? match.matchId || null : null,
        externalMatchId: match.externalMatchId || null,
        predefinedMatchId: match.predefinedMatchId || null,
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
        recordStatus: match.recordStatus || 'New',
        isVisible: match.isVisible ?? true,
        matchStatus: match.matchStatus || null,
        scoreHome: match.scoreHome ?? null,
        scoreAway: match.scoreAway ?? null,
        qualifiedTeam: match.qualifiedTeam ?? null
      })),

      users: this.usersArray.value.map((user: User) => ({
        assignmentId: user.assignmentId || null,
        userName: user.userName,
        userAdminName: user.userAdminName,
        userEmail: user.userEmail,
        status: user.status,
        userRole: user.userRole,
        recordStatus: user.recordStatus || 'New'
      })),

      settings: {
        allowExactResultBonus: this.tournamentForm.value.settings.allowExactResultBonus,
        exactResultBonusCalculation: this.tournamentForm.value.settings.exactResultBonusCalculation,
        exactResultBonus: this.tournamentForm.value.settings.exactResultBonus || null,
        allowWhoQualifiesBets: this.tournamentForm.value.settings.allowWhoQualifiesBets, 
        allowBetsWithBooster: this.tournamentForm.value.settings.allowBetsWithBooster,
        maxBetBooster: this.tournamentForm.value.settings.maxBetBooster || 1,
        totalBoosterPool: this.tournamentForm.value.settings.totalBoosterPool || null, 
        allowNonSubmittedBetsPenalty: this.tournamentForm.value.settings.allowNonSubmittedBetsPenalty,
        nonSubmittedBetPenalty: this.tournamentForm.value.settings.nonSubmittedBetPenalty || null,
      },
    };
    
    const submitObservable = isEditing
      ? this.tournamentService.updateCustomTournament(tournamentData)
      : this.tournamentService.createCustomTournament(tournamentData);
  
    submitObservable.subscribe({
      next: async (response) => {
        await this.tournamentSelectionService.loadSelectedTournamentFromBackend();

        await this.router.navigate(['/tournaments/custom']);
  
        this.showToast(
          isEditing ? 'Tournament updated successfully!' : 'Tournament created successfully!',
          'success'
        );
      },
      error: async (error) => {
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

  async handleTournamentUpdate(): Promise<void> {
    const loading = await this.loadingController.create({
      message: 'Refreshing data...',
      spinner: 'crescent',
    });
    await loading.present();
  
    try {
      const tournamentId = this.tournamentForm.value.tournamentId;
      if (!tournamentId) {
        this.showToast('Tournament ID is missing.', 'danger');
        await loading.dismiss();
        return;
      }
  
      const updatedTournament: Tournament = await firstValueFrom(
        this.tournamentService.checkForTournamentUpdates(tournamentId)
      );
    
      this.mergeUpdatedEntities(this.teamsArray, updatedTournament.teams, 'teamId');
      this.mergeUpdatedEntities(this.stagesArray, updatedTournament.stages, 'stageId');
      this.mergeUpdatedEntities(this.matchesArray, updatedTournament.matches, 'matchId');
  
      this.showToast('Tournament updated from source!', 'success');
    } catch (error) {
      this.showToast('Failed to refresh tournament!', 'danger');
    } finally {
      await loading.dismiss();
    }
  }

  mergeUpdatedEntities(formArray: FormArray, updatedList: any[], key: string): void {
    const currentMap = new Map(formArray.value.map((item: any) => [item[key], item]));
    const updatedMap = new Map(updatedList.map((item: any) => [item[key], item]));
  
    formArray.controls.forEach((control: AbstractControl) => {
      const formValue = control.value;
      const updated = updatedMap.get(formValue[key]);
  
      if (updated) {
        const patch: any = {};
  
        for (const [k, v] of Object.entries(updated)) {
          if (
            ['matchId', 'homeTeamId', 'awayTeamId', 'stageId'].includes(k)
          ) continue;
  
          if (formValue[k] !== v) {
            patch[k] = v;
          }
        }
  
        if (Object.keys(patch).length > 0) {
          (control as FormGroup).patchValue(patch);
        }
      } else {
        (control as FormGroup).patchValue({ recordStatus: 'Delete' });
      }
    });
  
    updatedList.forEach(item => {
      if (!currentMap.has(item[key])) {
        const newItem = {
          ...item,
          matchId: null,
          homeTeamId: null,
          awayTeamId: null,
          stageId: null,
          recordStatus: 'New'
        };
        formArray.push(this.fb.group(newItem));
      }
    });
  }
                   
  async nextStep(): Promise<void> {
    const canProceed = await this.canProceed();
    if (canProceed && this.step < 7) {
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
        const visibility = this.tournamentForm.get('tournamentVisibility')?.value;
        const name = this.tournamentForm.get('tournamentName')?.value?.trim();
  
        if (!name) {
          await this.showToast('Tournament Name is required!', 'danger');
          return false;
        }
  
        if (visibility?.toLowerCase() === 'public') {
          try {
            const response = await firstValueFrom(
              this.tournamentService.checkTournamentNameAvailability(name, visibility)
            );

            if (!response.available) {
              await this.showToast('This tournament name is already taken.', 'danger');
              return false;
            }
          } catch (error) {
            console.error('Error checking name availability:', error);
            await this.showToast('Could not validate tournament name.', 'danger');
            return false;
          }
        }
  
        return true;
  
      case 2:
        if (!this.settingsGroup.valid) {
          await this.showToast('Tournament settings are incomplete or invalid.', 'danger');
          return false;
        }
        return true;
  
      case 3:
        if (this.teamsArray.length <= 1) {
          await this.showToast('At least 2 teams are required!', 'danger');
          return false;
        }
        return true;
  
      case 4:
        if (this.stagesArray.length === 0) {
          await this.showToast('At least one stage is required!', 'danger');
          return false;
        }
        return true;
  
      case 5:
        if (this.matchesArray.length === 0) {
          await this.showToast('At least 1 match is required!', 'danger');
          return false;
        }
        return true;
  
      case 6:
        return true;
    }
  
    return false;
  }    

  private generateFrontendId(): string {
    return 'T-' + Math.random().toString(36).substr(2, 9);
  }
}