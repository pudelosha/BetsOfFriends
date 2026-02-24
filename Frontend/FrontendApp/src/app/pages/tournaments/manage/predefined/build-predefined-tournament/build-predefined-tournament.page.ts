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
  imports: [
    CommonModule,
    StageInputTypePage,
    StageTeamsManagementPage,
    StageStagesManagementPage,
    StageMatchesManagementPage,
    StageSummaryPage,
    TranslateModule,
    IonContent,
    IonButton,
    IonSpinner
  ],
})
export class BuildPredefinedTournamentPage implements OnInit {
  @ViewChild(StageTeamsManagementPage) stageTeamsManagement!: StageTeamsManagementPage;
  @ViewChild(StageMatchesManagementPage) stageMatchesManagement!: StageMatchesManagementPage;
  @ViewChild(StageStagesManagementPage) stageStagesManagement!: StageStagesManagementPage;

  tournamentForm: FormGroup;
  step = 1;
  tournamentId?: number | null = null;
  isLoading = false;
  isPredefinedTournament = true;

  constructor(
    private fb: FormBuilder,
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
      tournamentVisibility: ['Private', Validators.required],
      updateMethod: ['Manual', Validators.required],
      season: [null],
      seasonId: [null],
      tournamentStart: [null],
      tournamentEnd: [null],
      teams: this.fb.array([], Validators.required),
      stages: this.fb.array([]),
      matches: this.fb.array([]),
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
    const titleKey = this.isEditMode ? 'BUILD_PREDEFINED.EDIT_TITLE' : 'BUILD_PREDEFINED.TITLE';
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
    this.tournamentForm.reset({
      tournamentId: null,
      externalTournamentId: null,
      tournamentName: '',
      tournamentVisibility: 'Private',
      updateMethod: 'Manual',
      season: null,
      seasonId: null,
      tournamentStart: null,
      tournamentEnd: null,
    });
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
      },
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
          team.teamFrontendId = this.generateFrontendId();
        }

        teamMap.set(team.teamId ?? 0, team);

        this.teamsArray.push(
          this.fb.group({
            teamFrontendId: [team.teamFrontendId],
            teamId: [team.teamId],
            externalTeamId: [team.externalTeamId || null],
            predefinedTeamId: [team.predefinedTeamId ?? null],
            teamName: [team.teamName, Validators.required],
            eloRating: [
              team.eloRating ?? 1000,
              [Validators.required, Validators.min(0), Validators.max(5000)],
            ],
            recordStatus: ['Uploaded', Validators.required],
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
            stageName: [stage.stageName, Validators.required],
            order: [stage.order, [Validators.required, Validators.min(1)]],
            recordStatus: ['Uploaded', Validators.required],
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
          stageName: match.stageName || 'Default Stage',
        };

        this.matchesArray.push(
          this.fb.group({
            matchFrontendId: [match.matchFrontendId || this.generateFrontendId()],
            matchId: [match.matchId],
            externalMatchId: [match.externalMatchId || null],

            stageFrontendId: [stage.stageFrontendId],
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
              Validators.required,
            ],

            matchStatus: [match.matchStatus || null],
            scoreHome: [match.scoreHome ?? null],
            scoreAway: [match.scoreAway ?? null],
            qualifiedTeam: [match.qualifiedTeam ?? null],

            isVisible: [match.isVisible ?? true],
          })
        );
      });
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
      qualifiedTeam: [match.qualifiedTeam ?? null],

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
          predefinedTeamId: [team.predefinedTeamId ?? null],
          eloRating: [
            team.eloRating ?? 1000,
            [Validators.required, Validators.min(0), Validators.max(5000)],
          ],
          recordStatus: [team.recordStatus || 'New'],
        })
      );
    });
  }

  handleStagesExtracted(stages: Stage[]): void {
    this.stagesArray.clear();

    stages.forEach((stage) => {
      this.stagesArray.push(
        this.fb.group({
          stageFrontendId: [stage.stageFrontendId || this.generateFrontendId()],
          stageId: [stage.stageId],
          stageName: [stage.stageName, Validators.required],
          order: [stage.order, [Validators.required, Validators.min(1)]],
          recordStatus: [stage.recordStatus || 'New'],
        })
      );
    });
  }

  handleMatchesExtracted(matches: Match[]): void {
    this.matchesArray.clear();

    matches.forEach((match) => {
      this.matchesArray.push(
        this.buildMatchFormGroup({
          ...match,
          recordStatus: match.recordStatus || 'New',
        })
      );
    });
  }

  handleTeamsUpdated(teamsData: { previousTeams: Team[]; updatedTeams: Team[] }): void {
    const { previousTeams, updatedTeams } = teamsData;

    // Step 1: Create lookup maps for previous and updated teams
    const previousTeamMap = new Map(previousTeams.map((team) => [team.teamFrontendId, team]));
    const updatedTeamMap = new Map(updatedTeams.map((team) => [team.teamFrontendId, team]));

    // Step 2: Identify removed teams and mark them as "Delete"
    previousTeams.forEach((team) => {
      if (!updatedTeamMap.has(team.teamFrontendId)) {
        updatedTeams.push({ ...team, recordStatus: 'Delete' });
      }
    });

    // Step 3: Update teamsArray and handle name updates
    this.teamsArray.clear();
    updatedTeams.forEach((team) => {
      const previousTeam = previousTeamMap.get(team.teamFrontendId);
      const isUpdated =
        previousTeam &&
        (previousTeam.teamName !== team.teamName ||
          Number(previousTeam.eloRating ?? 1000) !== Number(team.eloRating ?? 1000));

      this.teamsArray.push(
        this.fb.group({
          teamFrontendId: [team.teamFrontendId],
          teamId: [team.teamId],
          externalTeamId: [team.externalTeamId ?? null],
          predefinedTeamId: [team.predefinedTeamId ?? null],
          teamName: [team.teamName, Validators.required],
          eloRating: [
            Number(team.eloRating ?? 1000),
            [Validators.required, Validators.min(0), Validators.max(5000)],
          ],
          recordStatus: [isUpdated ? 'Update' : team.recordStatus || 'New'],
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
    const validTeamFrontendIds = new Set(updatedTeams.map((team) => team.teamFrontendId));
    const matchesToKeep: AbstractControl[] = [];

    this.matchesArray.controls.forEach((control: AbstractControl) => {
      const match = (control as FormGroup).value;

      const homeTeam = updatedTeamMap.get(match.homeTeamFrontendId);
      const awayTeam = updatedTeamMap.get(match.awayTeamFrontendId);

      if (
        !validTeamFrontendIds.has(match.homeTeamFrontendId) ||
        !validTeamFrontendIds.has(match.awayTeamFrontendId)
      ) {
        //console.log(`Deleting match: ${match.homeTeam} vs ${match.awayTeam}`);
      } else if (homeTeam?.recordStatus === 'Delete' || awayTeam?.recordStatus === 'Delete') {
        //console.log(`Marking match as 'Delete': ${match.homeTeam} vs ${match.awayTeam}`);
        (control as FormGroup).patchValue({ recordStatus: 'Delete' });
        matchesToKeep.push(control);
      } else if (
        previousTeamMap.get(match.homeTeamFrontendId)?.recordStatus === 'Delete' ||
        previousTeamMap.get(match.awayTeamFrontendId)?.recordStatus === 'Delete'
      ) {
        //console.log(`Restoring match: ${match.homeTeam} vs ${match.awayTeam}`);
        (control as FormGroup).patchValue({ recordStatus: 'Update' });
        matchesToKeep.push(control);
      } else {
        matchesToKeep.push(control);
      }
    });

    // Step 5: Rebuild matchesArray with only valid matches
    this.matchesArray.clear();
    matchesToKeep.forEach((control) => this.matchesArray.push(control));

    // Step 6: Update odds for matches affected by Elo changes:
    this.recalculateOddsForMatchesAffectedByEloChange(previousTeams, updatedTeams);
  }

  handleStagesUpdated(stagesData: { previousStages: Stage[]; updatedStages: Stage[] }): void {
    const { previousStages, updatedStages } = stagesData;

    // Step 1: Create lookup maps for previous and updated stages
    const previousStageMap = new Map(previousStages.map((stage) => [stage.stageFrontendId, stage]));
    const updatedStageMap = new Map(updatedStages.map((stage) => [stage.stageFrontendId, stage]));

    // Step 2: Identify removed stages and mark them as "Delete"
    previousStages.forEach((stage) => {
      if (!updatedStageMap.has(stage.stageFrontendId)) {
        updatedStages.push({ ...stage, recordStatus: 'Delete' });
      }
    });

    // Step 3: Update stagesArray and detect changes
    this.stagesArray.clear();
    updatedStages.forEach((stage) => {
      const previousStage = previousStageMap.get(stage.stageFrontendId);
      const isUpdated =
        previousStage && (previousStage.stageName !== stage.stageName || previousStage.order !== stage.order);

      this.stagesArray.push(
        this.fb.group({
          stageFrontendId: [stage.stageFrontendId],
          stageId: [stage.stageId],
          stageName: [stage.stageName, Validators.required],
          order: [stage.order, [Validators.required, Validators.min(1)]],
          recordStatus: [isUpdated ? 'Update' : stage.recordStatus || 'New'],
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
    const validStageFrontendIds = new Set(updatedStages.map((stage) => stage.stageFrontendId));
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
    matchesToKeep.forEach((control) => this.matchesArray.push(control));
  }

  handleMatchesUpdated(updatedMatches: Match[]): void {
    const previousMatches: Match[] = this.matchesArray.value;
    const previousMatchMap = new Map(previousMatches.map((match) => [match.matchFrontendId, match]));

    // Step 1: Identify deleted matches
    previousMatches.forEach((match) => {
      if (!updatedMatches.some((updated) => updated.matchFrontendId === match.matchFrontendId)) {
        updatedMatches.push({ ...match, recordStatus: 'Delete' });
      }
    });

    // Step 2: Detect updates & rebuild matchesArray
    this.matchesArray.clear();
    updatedMatches.forEach((match) => {
      const previousMatch = previousMatchMap.get(match.matchFrontendId);
      const isUpdated =
        previousMatch &&
        (previousMatch.stageFrontendId !== match.stageFrontendId ||
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
          previousMatch.scoreAway !== match.scoreAway ||
          previousMatch.qualifiedTeam !== match.qualifiedTeam);

      this.matchesArray.push(
        this.fb.group({
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
          qualifiedTeam: [match.qualifiedTeam ?? null],
          recordStatus: [isUpdated ? 'Update' : match.recordStatus || 'New'],
        })
      );
    });

    // Step 3: Handle match restoration (UNDO)
    updatedMatches.forEach((match) => {
      if (match.recordStatus === 'Update') {
        this.teamsArray.controls.forEach((control) => {
          const team = control.value;
          if (
            (team.teamFrontendId === match.homeTeamFrontendId ||
              team.teamFrontendId === match.awayTeamFrontendId) &&
            team.recordStatus === 'Delete'
          ) {
            (control as FormGroup).patchValue({ recordStatus: 'Update' });
          }
        });

        this.stagesArray.controls.forEach((control) => {
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
        predefinedTeamId: team.predefinedTeamId ?? null,
        externalTeamId: team.externalTeamId ?? null,
        teamName: team.teamName,
        eloRating: Number(team.eloRating ?? 1000),
        recordStatus: team.recordStatus || 'New',
      })),

      stages: this.stagesArray.value.map((stage: Stage) => ({
        stageId: isEditing ? stage.stageId || null : null,
        stageName: stage.stageName,
        order: stage.order,
        recordStatus: stage.recordStatus || 'New',
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
        qualifiedTeam: match.qualifiedTeam ?? null,
        isVisible: match.isVisible ?? true,
        recordStatus: match.recordStatus || 'New',
      })),
    };

    const submitObservable = isEditing
      ? this.tournamentService.updatePredefinedTournament(tournamentData)
      : this.tournamentService.createPredefinedTournament(tournamentData);

    submitObservable.subscribe({
      next: async (response) => {
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

  async showToast(
    message: string,
    color: 'success' | 'warning' | 'danger' | 'primary'
  ): Promise<void> {
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
        return true;
    }
    return false;
  }

  private generateFrontendId(): string {
    return 'T-' + Math.random().toString(36).substr(2, 9);
  }

  private recalculateOddsForMatchesAffectedByEloChange(
    previousTeams: Team[],
    updatedTeams: Team[]
  ): void {
    // Fixed constant for now (same as your Excel "-50" term)
    const HOME_ADVANTAGE = 50;

    const round2 = (v: number) => Math.round(v * 100) / 100;
    const clamp = (v: number, min: number, max: number) => Math.max(min, Math.min(max, v));

    // Build maps for ELO lookup by frontendId
    const prevEloByFrontendId = new Map<string, number>();
    previousTeams.forEach((t) => {
      const elo = Number(t.eloRating ?? 1000);
      if (t.teamFrontendId) prevEloByFrontendId.set(t.teamFrontendId, elo);
    });

    const nextEloByFrontendId = new Map<string, number>();
    updatedTeams.forEach((t) => {
      const elo = Number(t.eloRating ?? 1000);
      if (t.teamFrontendId) nextEloByFrontendId.set(t.teamFrontendId, elo);
    });

    // Determine which teams actually changed ELO
    const changedTeamFrontendIds = new Set<string>();
    updatedTeams.forEach((t) => {
      const id = t.teamFrontendId;
      if (!id) return;

      const prev = prevEloByFrontendId.get(id);
      const next = nextEloByFrontendId.get(id);

      // Treat missing prev as "no baseline" -> don't trigger odds recalc
      if (prev !== undefined && next !== undefined && prev !== next) {
        changedTeamFrontendIds.add(id);
      }
    });

    if (changedTeamFrontendIds.size === 0) return;

    // Iterate matches and update odds where needed
    this.matchesArray.controls.forEach((control: AbstractControl) => {
      const fg = control as FormGroup;
      const match = fg.value as any;

      const recordStatus: string | null = match.recordStatus ?? null;
      const matchStatus: string | null = match.matchStatus ?? null;

      // Skip deleted or finished/finalised matches
      const isFinished =
        recordStatus === 'Finalised' || (matchStatus ?? '').toLowerCase() === 'finished';

      if (recordStatus === 'Delete' || isFinished) return;

      const homeId: string | null = match.homeTeamFrontendId ?? null;
      const awayId: string | null = match.awayTeamFrontendId ?? null;

      // Allow placeholders: if missing/temporary ids -> skip odds update
      if (!homeId || !awayId) return;

      // Only recalc if either team's ELO changed
      if (!changedTeamFrontendIds.has(homeId) && !changedTeamFrontendIds.has(awayId)) return;

      // Resolve ELOs (if placeholders not yet resolvable -> skip)
      const homeElo = nextEloByFrontendId.get(homeId);
      const awayElo = nextEloByFrontendId.get(awayId);
      if (homeElo === undefined || awayElo === undefined) return;

      // === Excel formulas ===
      // PowerRatio = 1 / (1 + 10^(((AwayElo - HomeElo - 50)/600)))
      const powerRatio = 1 / (1 + Math.pow(10, (awayElo - homeElo - HOME_ADVANTAGE) / 600));

      // ProbDraw = MAX(0, MIN(0.33, 0.29 - ABS(0.5 - PowerRatio)*0.3))
      const probDraw = clamp(0.29 - Math.abs(0.5 - powerRatio) * 0.3, 0, 0.33);

      // ProbHome = (1 - ProbDraw) * PowerRatio
      const probHome = (1 - probDraw) * powerRatio;

      // ProbAway = 1 - ProbHome - ProbDraw
      const probAway = 1 - probHome - probDraw;

      // Guard: avoid division by 0 / negative probs
      if (!(probHome > 0) || !(probDraw > 0) || !(probAway > 0)) return;

      // Odds = ROUND(1/Prob*(0.95 + RAND()/100), 2)
      const jitter = () => 0.95 + Math.random() / 100;

      const odds1 = round2((1 / probHome) * jitter());
      const oddsX = round2((1 / probDraw) * jitter());
      const odds2 = round2((1 / probAway) * jitter());

      const patch: any = {
        homeWinOdds: odds1,
        drawOdds: oddsX,
        awayWinOdds: odds2,
      };

      // Mark existing backend records as needing update (don’t change New)
      if (recordStatus === 'Uploaded') {
        patch.recordStatus = 'Update';
      }

      fg.patchValue(patch);
    });
  }
}
