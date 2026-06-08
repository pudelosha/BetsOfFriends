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
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { TitleService } from 'src/app/services/title.service';
import { IonContent, IonButton, IonSpinner } from '@ionic/angular/standalone';
import {
  calculateEloMatchOdds,
  calculateEloQualificationOdds,
} from 'src/app/pages/tournaments/shared/odds-utils';

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
    private titleService: TitleService,
    private translate: TranslateService
  ) {
    this.tournamentForm = this.fb.group({
      tournamentId: [null],
      externalTournamentId: [null],
      tournamentName: ['', [Validators.required, Validators.maxLength(50)]],
      tournamentVisibility: ['Private', Validators.required],
      updateMethod: ['Manual', Validators.required],
      includeHomeAdvantage: [true],
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

    this.tournamentForm.get('includeHomeAdvantage')?.valueChanges.subscribe(() => {
      this.recalculateOddsForAllEligibleMatches();
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

  private isHomeAdvantageEnabled(): boolean {
    return this.tournamentForm.get('includeHomeAdvantage')?.value === true;
  }

  private resetFormData(): void {
    this.tournamentForm.reset({
      tournamentId: null,
      externalTournamentId: null,
      tournamentName: '',
      tournamentVisibility: 'Private',
      updateMethod: 'Manual',
      includeHomeAdvantage: true,
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
      message: this.t('TOASTS.LOADING_TOURNAMENT'),
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
        includeHomeAdvantage: tournament.includeHomeAdvantage ?? true,
        season: tournament.season || null,
        seasonId: tournament.seasonId || null,
        tournamentStart: tournament.tournamentStart || null,
        tournamentEnd: tournament.tournamentEnd || null,
      });

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

    this.recalculateOddsForAllEligibleMatches();
  }

  handleTeamsUpdated(teamsData: { previousTeams: Team[]; updatedTeams: Team[] }): void {
    const { previousTeams, updatedTeams } = teamsData;

    const previousTeamMap = new Map(previousTeams.map((team) => [team.teamFrontendId, team]));
    const updatedTeamMap = new Map(updatedTeams.map((team) => [team.teamFrontendId, team]));

    previousTeams.forEach((team) => {
      if (!updatedTeamMap.has(team.teamFrontendId)) {
        updatedTeams.push({ ...team, recordStatus: 'Delete' });
      }
    });

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
      } else if (homeTeam?.recordStatus === 'Delete' || awayTeam?.recordStatus === 'Delete') {
        (control as FormGroup).patchValue({ recordStatus: 'Delete' });
        matchesToKeep.push(control);
      } else if (
        previousTeamMap.get(match.homeTeamFrontendId)?.recordStatus === 'Delete' ||
        previousTeamMap.get(match.awayTeamFrontendId)?.recordStatus === 'Delete'
      ) {
        (control as FormGroup).patchValue({ recordStatus: 'Update' });
        matchesToKeep.push(control);
      } else {
        matchesToKeep.push(control);
      }
    });

    this.matchesArray.clear();
    matchesToKeep.forEach((control) => this.matchesArray.push(control));

    this.recalculateOddsForMatchesAffectedByEloChange(previousTeams, updatedTeams);
  }

  handleStagesUpdated(stagesData: { previousStages: Stage[]; updatedStages: Stage[] }): void {
    const { previousStages, updatedStages } = stagesData;

    const previousStageMap = new Map(previousStages.map((stage) => [stage.stageFrontendId, stage]));
    const updatedStageMap = new Map(updatedStages.map((stage) => [stage.stageFrontendId, stage]));

    previousStages.forEach((stage) => {
      if (!updatedStageMap.has(stage.stageFrontendId)) {
        updatedStages.push({ ...stage, recordStatus: 'Delete' });
      }
    });

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

    const validStageFrontendIds = new Set(updatedStages.map((stage) => stage.stageFrontendId));
    const matchesToKeep: AbstractControl[] = [];

    this.matchesArray.controls.forEach((control: AbstractControl) => {
      const match = (control as FormGroup).value;
      const stage = updatedStageMap.get(match.stageFrontendId);

      if (!validStageFrontendIds.has(match.stageFrontendId)) {
      } else if (stage?.recordStatus === 'Delete') {
        (control as FormGroup).patchValue({ recordStatus: 'Delete' });
        matchesToKeep.push(control);
      } else if (previousStageMap.get(match.stageFrontendId)?.recordStatus === 'Delete') {
        (control as FormGroup).patchValue({ recordStatus: 'Update' });
        matchesToKeep.push(control);
      } else {
        matchesToKeep.push(control);
      }
    });

    this.matchesArray.clear();
    matchesToKeep.forEach((control) => this.matchesArray.push(control));
  }

  handleMatchesUpdated(updatedMatches: Match[]): void {
    const previousMatches: Match[] = this.matchesArray.value;
    const previousMatchMap = new Map(previousMatches.map((match) => [match.matchFrontendId, match]));

    previousMatches.forEach((match) => {
      if (!updatedMatches.some((updated) => updated.matchFrontendId === match.matchFrontendId)) {
        updatedMatches.push({ ...match, recordStatus: 'Delete' });
      }
    });

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
      this.showToast(this.t('TOASTS.TOURNAMENT_NAME_REQUIRED'), 'danger');
      return;
    }

    if (this.teamsArray.length < 2) {
      this.showToast(this.t('TOASTS.TEAMS_MIN_CREATE'), 'danger');
      return;
    }

    if (this.stagesArray.length === 0) {
      this.showToast(this.t('TOASTS.STAGE_REQUIRED'), 'danger');
      return;
    }

    if (this.matchesArray.length < 1) {
      this.showToast(this.t('TOASTS.MATCH_REQUIRED'), 'danger');
      return;
    }

    const loading = await this.loadingController.create({
      message: this.t('TOASTS.SUBMITTING_TOURNAMENT'),
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
      includeHomeAdvantage: this.isHomeAdvantageEnabled(),
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

      matches: this.matchesArray.value.map((match: any) => {
        const isQualificationMatch = match.matchType === 'ExtendedWithQualification';

        return {
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
          homeQualifies: isQualificationMatch ? match.homeQualifies : null,
          awayQualifies: isQualificationMatch ? match.awayQualifies : null,
          matchStatus: match.matchStatus || null,
          scoreHome: match.scoreHome ?? null,
          scoreAway: match.scoreAway ?? null,
          qualifiedTeam: match.qualifiedTeam ?? null,
          isVisible: match.isVisible ?? true,
          recordStatus: match.recordStatus || 'New',
        };
      }),
    };

    const submitObservable = isEditing
      ? this.tournamentService.updatePredefinedTournament(tournamentData)
      : this.tournamentService.createPredefinedTournament(tournamentData);

    submitObservable.subscribe({
      next: async (response) => {
        await this.router.navigate(['/tournaments/predefined']);

        this.showToast(
          this.t(isEditing ? 'TOASTS.TOURNAMENT_UPDATED' : 'TOASTS.TOURNAMENT_CREATED'),
          'success'
        );
      },
      error: async (error) => {
        console.error('Error submitting tournament:', error);
        this.showToast(this.t('TOASTS.TOURNAMENT_SUBMIT_FAILED'), 'danger');
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
          await this.showToast(this.t('TOASTS.TOURNAMENT_NAME_REQUIRED'), 'danger');
          return false;
        }
        return true;

      case 2:
        if (this.teamsArray.length <= 1) {
          await this.showToast(this.t('TOASTS.TEAMS_MIN'), 'danger');
          return false;
        }
        return true;

      case 3:
        if (this.stagesArray.length === 0) {
          await this.showToast(this.t('TOASTS.STAGES_MIN'), 'danger');
          return false;
        }
        return true;

      case 4:
        if (this.matchesArray.length === 0) {
          await this.showToast(this.t('TOASTS.MATCH_REQUIRED'), 'danger');
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

  private t(key: string, params?: Record<string, unknown>): string {
    return this.translate.instant(key, params);
  }

  private getEloByFrontendId(): Map<string, number> {
    const eloByFrontendId = new Map<string, number>();
    this.teamsArray.controls.forEach((control: AbstractControl) => {
      const team = control.value;
      const id = team.teamFrontendId;
      const elo = Number(team.eloRating ?? 1000);

      if (id) {
        eloByFrontendId.set(id, elo);
      }
    });

    return eloByFrontendId;
  }

  private shouldSkipOddsRecalculation(match: any): boolean {
    const recordStatus: string | null = match.recordStatus ?? null;
    const matchStatus: string | null = match.matchStatus ?? null;

    const isFinished =
      recordStatus === 'Finalised' || (matchStatus ?? '').toLowerCase() === 'finished';

    return recordStatus === 'Delete' || isFinished;
  }

  private patchCalculatedOdds(
    fg: FormGroup,
    match: any,
    homeElo: number,
    awayElo: number
  ): void {
    const includeHomeAdvantage = this.isHomeAdvantageEnabled();
    const matchOdds = calculateEloMatchOdds(homeElo, awayElo, includeHomeAdvantage);
    if (!matchOdds) return;

    const patch: any = {
      homeWinOdds: matchOdds.homeWinOdds,
      drawOdds: matchOdds.drawOdds,
      awayWinOdds: matchOdds.awayWinOdds,
    };

    if (match.matchType === 'ExtendedWithQualification') {
      const qualificationOdds = calculateEloQualificationOdds(homeElo, awayElo, includeHomeAdvantage);

      if (qualificationOdds) {
        patch.homeQualifies = qualificationOdds.homeQualifies;
        patch.awayQualifies = qualificationOdds.awayQualifies;
      }
    } else {
      patch.homeQualifies = null;
      patch.awayQualifies = null;
    }

    if (match.recordStatus === 'Uploaded') {
      patch.recordStatus = 'Update';
    }

    fg.patchValue(patch, { emitEvent: false });
  }

  private recalculateOddsForAllEligibleMatches(): void {
    const eloByFrontendId = this.getEloByFrontendId();

    this.matchesArray.controls.forEach((control: AbstractControl) => {
      const fg = control as FormGroup;
      const match = fg.value as any;

      if (this.shouldSkipOddsRecalculation(match)) return;

      const homeId: string | null = match.homeTeamFrontendId ?? null;
      const awayId: string | null = match.awayTeamFrontendId ?? null;

      if (!homeId || !awayId) return;

      const homeElo = eloByFrontendId.get(homeId);
      const awayElo = eloByFrontendId.get(awayId);

      if (homeElo === undefined || awayElo === undefined) return;

      this.patchCalculatedOdds(fg, match, homeElo, awayElo);
    });
  }

  private recalculateOddsForMatchesAffectedByEloChange(
    previousTeams: Team[],
    updatedTeams: Team[]
  ): void {
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

    const changedTeamFrontendIds = new Set<string>();
    updatedTeams.forEach((t) => {
      const id = t.teamFrontendId;
      if (!id) return;

      const prev = prevEloByFrontendId.get(id);
      const next = nextEloByFrontendId.get(id);

      if (prev !== undefined && next !== undefined && prev !== next) {
        changedTeamFrontendIds.add(id);
      }
    });

    if (changedTeamFrontendIds.size === 0) return;

    this.matchesArray.controls.forEach((control: AbstractControl) => {
      const fg = control as FormGroup;
      const match = fg.value as any;

      if (this.shouldSkipOddsRecalculation(match)) return;

      const homeId: string | null = match.homeTeamFrontendId ?? null;
      const awayId: string | null = match.awayTeamFrontendId ?? null;

      if (!homeId || !awayId) return;

      if (!changedTeamFrontendIds.has(homeId) && !changedTeamFrontendIds.has(awayId)) return;

      const homeElo = nextEloByFrontendId.get(homeId);
      const awayElo = nextEloByFrontendId.get(awayId);
      if (homeElo === undefined || awayElo === undefined) return;

      this.patchCalculatedOdds(fg, match, homeElo, awayElo);
    });
  }
}
