import { Component, Input, Output, EventEmitter, ViewChild, ElementRef, OnInit } from '@angular/core';
import * as XLSX from 'xlsx';
import { FormGroup, ReactiveFormsModule, FormArray } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Tournament, Team, Match, Stage } from 'src/app/model/tournament-model';
import { PredefinedTournamentService } from 'src/app/services/predefined-tournament.service';
import { ModalController } from '@ionic/angular';
import { TournamentSelectionModalComponent } from 'src/app/modals/tournament-selection-modal/tournament-selection.modal';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { SelectCompetitionModalComponent } from 'src/app/modals/select-competition-modal/select-competition-modal.component';
import { ExternalDataService } from 'src/app/services/external-data.service';
import { ToastController, AlertController, LoadingController } from '@ionic/angular';
import { IonGrid, IonRow, IonCol, IonLabel, IonButton, IonIcon, IonPopover, IonContent, IonSegment, IonSegmentButton, IonInput, IonSelect, IonSelectOption, IonDatetime, IonDatetimeButton, IonModal } from '@ionic/angular/standalone';
import { FormControl } from '@angular/forms';

@Component({
  selector: 'app-stage-input-type',
  templateUrl: './stage-input-type.page.html',
  styleUrls: ['./stage-input-type.page.scss'],
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, TranslateModule, IonGrid, IonRow, IonCol, IonLabel, IonButton, IonIcon, IonPopover, IonContent, IonSegment, IonSegmentButton, IonInput, IonSelect, IonSelectOption, IonDatetime, IonDatetimeButton, IonModal],
})
export class StageInputTypePage implements OnInit {
  @Input() tournamentForm!: FormGroup;
  @Input() isEditMode: boolean = false;
  @Input() isPredefinedTournament: boolean = false;
  @Output() teamsExtracted = new EventEmitter<Team[]>(); 
  @Output() stagesExtracted = new EventEmitter<Stage[]>();
  @Output() matchesExtracted = new EventEmitter<Match[]>(); 
  @Output() tournamentSelected = new EventEmitter<Tournament>();
  @Output() tournamentUpdateRequested = new EventEmitter<void>();
  @ViewChild('fileInput') fileInput!: ElementRef;
  @ViewChild('betsFileInput') betsFileInput!: ElementRef;

  file: File | null = null;
  betsFile: File | null = null;
  predefinedTournaments: Tournament[] = [];
  selectedTournamentId: number | null = null;
  isLoading: boolean = false;

  constructor(
    private toastController: ToastController, 
    private tournamentService: PredefinedTournamentService, 
    private modalController: ModalController,
    private alertController: AlertController,
    private loadingController: LoadingController,
    private externalDataService: ExternalDataService,
    private translate: TranslateService
  ) {}

  ngOnInit(): void {

    // Ensure home advantage toggle exists in the form as a BOOLEAN
    if (!this.tournamentForm.get('includeHomeAdvantage')) {
      this.tournamentForm.addControl('includeHomeAdvantage', new FormControl(true));
    } else {
      const currentValue = this.tournamentForm.get('includeHomeAdvantage')?.value;
      this.tournamentForm.patchValue({
        includeHomeAdvantage: currentValue === true || currentValue === 'true'
      });
    }

    if (this.isCustomTournamentCreateMode) {
      this.tournamentForm.patchValue({
        tournamentVisibility: 'Private',
        updateMethod: 'Manual',
      });
    } else if (!this.isEditMode && this.isPredefinedTournament) {
      this.tournamentForm.patchValue({
        updateMethod: 'Manual',
      });
    }

    if (this.isCustomTournamentCreateMode) {
      this.loadPredefinedTournaments();
    }
  }

  onHomeAdvantageChange(event: any): void {
    const value = event?.detail?.value === 'true';
    this.tournamentForm.get('includeHomeAdvantage')?.setValue(value);
  }
          
  loadPredefinedTournaments(): void {
    this.tournamentService.getActivePredefinedTournaments().subscribe({
      next: (tournaments) => {
        this.predefinedTournaments = tournaments;
      },
      error: (err) => {
        console.error('Error loading predefined tournaments:', err);
      },
    });
  }

  get isCustomTournamentCreateMode(): boolean {
    return !this.isEditMode && !this.isPredefinedTournament;
  }
  
  get isCustomTournamentEditMode(): boolean {
    return this.isEditMode && !this.isPredefinedTournament;
  }

  get isCreatePredefined(): boolean {
    return !this.isEditMode && this.isPredefinedTournament;
  }
  
  get isEditPredefined(): boolean {
    return this.isEditMode && this.isPredefinedTournament;
  }
    
  async openTournamentSelection(): Promise<void> {
    const modal = await this.modalController.create({
      component: TournamentSelectionModalComponent,
      componentProps: { predefinedTournaments: this.predefinedTournaments },
      breakpoints: [0, 0.5, 1],
      initialBreakpoint: 0.5,
    });
  
    await modal.present();
  
    const { data } = await modal.onWillDismiss();
    this.selectedTournamentId = data?.selectedTournamentId ?? null;
  
    if (this.selectedTournamentId !== null) {
      const loading = await this.loadingController.create({
        message: this.t('TOASTS.LOADING_TOURNAMENT'),
        spinner: 'crescent',
      });
      await loading.present();
  
      const startTime = Date.now();
  
      this.tournamentService.getPredefinedTournamentById(this.selectedTournamentId).subscribe({
        next: async (tournament) => {
          tournament.predefinedTournamentId = tournament?.tournamentId ?? null;
  
          tournament.teams = tournament.teams.map(team => ({
            ...team,
            recordStatus: 'New',
            predefinedTeamId: team.teamId,
          }));
          tournament.stages = tournament.stages.map(stage => ({
            ...stage,
            recordStatus: 'New',
            predefinedStageId: stage.stageId,
          }));
          tournament.matches = tournament.matches.map(match => ({
            ...match,
            recordStatus: 'New',
            predefinedMatchId: match.matchId,
          }));
  
          this.tournamentSelected.emit(tournament);

          this.tournamentForm.patchValue({
            updateMethod: 'Auto',
          });

          this.showToast(this.t('TOASTS.TOURNAMENT_LOADED'), 'success');
        },
        error: async (err) => {
          console.error('Error fetching tournament:', err);
          this.showToast(this.t('TOASTS.TOURNAMENT_LOAD_FAILED'), 'danger');
        },
        complete: async () => {
          const elapsedTime = Date.now() - startTime;
          const delay = Math.max(0, 1000 - elapsedTime);
          setTimeout(async () => {
            await loading.dismiss();
          }, delay);
        }
      });
    } else {
      this.showToast(this.t('TOASTS.NO_TOURNAMENT_SELECTED'), 'warning');
    }
  }
  
  handleFileInput(event: any) {
    this.file = event.target.files[0];
    if (this.file) {
      this.readExcelFile();
    }
  }

  handleBetsFileInput(event: any) {
    this.betsFile = event.target.files[0];
    if (this.betsFile) {
      this.readBetsExcelFile();
    }
  }

  triggerFileInput(): void {
    this.fileInput.nativeElement.click();
  }

  triggerBetsFileInput(): void {
    this.betsFileInput.nativeElement.click();
  }

  async openAPISelection() {
    const modal = await this.modalController.create({
      component: SelectCompetitionModalComponent,
      breakpoints: [0, 0.5, 1],
      initialBreakpoint: 0.5,
    });
  
    await modal.present();
  
    const { data } = await modal.onWillDismiss();
  
    if (data?.competitionCode && data?.seasonCode) {
      const competitionCode = data.competitionCode;
      const seasonCode = data.seasonCode;
    
      const loading = await this.loadingController.create({
        message: this.t('TOASTS.IMPORTING_COMPETITION_DATA'),
        spinner: 'crescent',
      });
      await loading.present();
  
      const startTime = Date.now();
  
      this.externalDataService.getCompetitionMatches(competitionCode, seasonCode).subscribe({
        next: (tournament) => {  
          const teams: Team[] = tournament.teams.map(t => ({
            teamFrontendId: this.generateFrontendId(),
            teamId: null,
            externalTeamId: t.externalTeamId,
            predefinedTeamId: t.teamId,
            crestUrl: this.resolveTeamCrestUrl(t),
            teamName: t.teamName,
            recordStatus: 'New'
          }));
  
          const stages: Stage[] = tournament.stages.map((s, index) => ({
            stageFrontendId: this.generateFrontendId(),
            stageId: null,
            predefinedStageId: s.stageId,
            stageName: s.stageName,
            order: index + 1,
            recordStatus: 'New'
          }));
  
          const stageMap = new Map(stages.map(s => [s.stageName.toLowerCase(), s]));
          const teamMap = new Map(teams.map(t => [t.teamName.toLowerCase(), t]));
  
          const matches: Match[] = tournament.matches.map(m => {
            const stage = stageMap.get(m.stageName.toLowerCase());
            const home = teamMap.get(m.homeTeam.toLowerCase());
            const away = teamMap.get(m.awayTeam.toLowerCase());
  
            return {
              matchFrontendId: this.generateFrontendId(),
              matchId: null,
              externalMatchId: m.externalMatchId,
              predefinedMatchId: m.matchId,
  
              stageFrontendId: stage?.stageFrontendId ?? this.generateFrontendId(),
              stageId: stage?.stageId ?? null,
              stageName: m.stageName,
  
              homeTeamId: home?.teamId ?? null,
              homeTeamFrontendId: home?.teamFrontendId ?? this.generateFrontendId(),
              homeTeam: m.homeTeam,
  
              awayTeamId: away?.teamId ?? null,
              awayTeamFrontendId: away?.teamFrontendId ?? this.generateFrontendId(),
              awayTeam: m.awayTeam,
  
              matchStart: new Date(m.matchStart).toISOString(),
              matchType: m.matchType,
              homeWinOdds: m.homeWinOdds,
              drawOdds: m.drawOdds,
              awayWinOdds: m.awayWinOdds,
              homeQualifies: m.homeQualifies,
              awayQualifies: m.awayQualifies,
              isVisible: m.isVisible,
  
              matchStatus: m.matchStatus,
              scoreHome: m.scoreHome,
              scoreAway: m.scoreAway,
  
              recordStatus: m.matchStatus?.toLowerCase() === 'finished' ? 'Finalised' : 'New'
            };
          });
  
          this.teamsExtracted.emit(teams);
          this.stagesExtracted.emit(stages);
          this.matchesExtracted.emit(matches);
  
          this.tournamentForm.patchValue({
            updateMethod: 'Auto',
            tournamentName: tournament.tournamentName ?? null,
            externalTournamentId: tournament.externalTournamentId ?? null,
            season: tournament.season ?? null,
            seasonId: tournament.seasonId ?? null,
            tournamentStart: tournament.tournamentStart ?? null,
            tournamentEnd: tournament.tournamentEnd ?? null
          });
  
          this.showToast(this.t('TOASTS.COMPETITION_LOADED'), 'success');
        },
        error: (err) => {
          console.error('Error fetching competition matches:', err);
          this.showToast(this.t('TOASTS.COMPETITION_LOAD_FAILED'), 'danger');
        },
        complete: async () => {
          const elapsedTime = Date.now() - startTime;
          const delay = Math.max(0, 800 - elapsedTime);
          setTimeout(async () => {
            await loading.dismiss();
          }, delay);
        }
      });
  
    } else {
      this.showToast(this.t('TOASTS.NO_COMPETITION_SELECTED'), 'warning');
    }
  }  
  
  async triggerTournamentUpdate(): Promise<void> {
    const alert = await this.alertController.create({
      header: this.t('TOASTS.CONFIRM_UPDATE_TITLE'),
      message: this.t('TOASTS.CONFIRM_UPDATE_MESSAGE'),
      buttons: [
        {
          text: this.t('TOASTS.CANCEL'),
          role: 'cancel',
        },
        {
          text: this.t('TOASTS.UPDATE'),
          handler: () => {
            this.tournamentUpdateRequested.emit();
          },
        },
      ],
    });
  
    await alert.present();
  }

  async triggerAPIUpdate(): Promise<void> {

  }

  async downloadExcel() {
    const tournamentId = this.tournamentForm.get('tournamentId')?.value;
    const tournamentName = this.tournamentForm.get('tournamentName')?.value;

    if (!tournamentId ) {
      await this.showToast(this.t('TOASTS.NO_TOURNAMENT_SELECTED'), 'danger');
      return;
    }
  
    this.tournamentService.downloadExcel(tournamentId )
      .subscribe({
        next: async (blob) => {
          const a = document.createElement('a');
          const url = window.URL.createObjectURL(blob);
          a.href = url;
          a.download = `matches_${tournamentName}.xlsx`;
          a.click();
          window.URL.revokeObjectURL(url);
  
          await this.showToast(this.t('TOASTS.EXCEL_DOWNLOADED'), 'success');
        },
        error: async (err) => {
          console.error('Error downloading Excel file:', err);
          await this.showToast(this.t('TOASTS.EXCEL_DOWNLOAD_FAILED'), 'danger');
        }
      });
  }
  
  async showToast(message: string, color: 'success' | 'warning' | 'danger' | 'primary' = 'primary') {
    const toast = await this.toastController.create({
      message,
      duration: 3000,
      position: 'bottom',
      color,
    });
    await toast.present();
  }

  readExcelFile() {
    const reader = new FileReader();

    reader.onload = (e: any) => {
      try {
        const data = new Uint8Array(e.target.result);
        const workbook = XLSX.read(data, { type: 'array' });

        const teams = this.extractTeams(workbook);
        const stages = this.extractStages(workbook);
        const matches = this.extractMatches(workbook, teams, stages);

        this.teamsExtracted.emit(teams);
        this.stagesExtracted.emit(stages);
        this.matchesExtracted.emit(matches);

        this.tournamentForm.patchValue({ updateMethod: 'Manual'});

        this.showToast(this.t('TOASTS.EXCEL_READ_SUCCESS'), 'success');
      } catch (error) {
        this.showToast(this.t('TOASTS.EXCEL_READ_FAILED'), 'danger');
        console.error('Excel read error:', error);
      }
    };

    reader.readAsArrayBuffer(this.file!);
  }

  readBetsExcelFile() {
    if (!this.betsFile) {
      this.showToast(this.t('TOASTS.NO_FILE_SELECTED'), 'warning');
      return;
    }
  
    const reader = new FileReader();
  
    reader.onload = (e: any) => {
      try {
        const data = new Uint8Array(e.target.result);
        const workbook = XLSX.read(data, { type: 'array' });
  
        const sheet = workbook.Sheets['Matches'];
        if (!sheet) {
          this.showToast(this.t('TOASTS.EXCEL_NO_MATCHES_SHEET'), 'danger');
          return;
        }
  
        const rows = XLSX.utils.sheet_to_json(sheet);
  
        const matches: Match[] = this.tournamentForm.get('matches')?.value ?? [];
        let updates = 0;
  
        rows.forEach((row: any) => {
          const matchId = Number(row['MatchId']);
  
          if (!matchId) return;
  
          const matchIndex = matches.findIndex(m => m.matchId === matchId);
  
          if (matchIndex >= 0) {
            const updatedMatch = { ...matches[matchIndex] };
  
            updatedMatch.homeWinOdds = this.parseOdds(row['HomeWinOdds']);
            updatedMatch.drawOdds = this.parseOdds(row['DrawOdds']);
            updatedMatch.awayWinOdds = this.parseOdds(row['AwayWinOdds']);
            updatedMatch.homeQualifies = this.parseOptionalQualifier(row['HomeQualifies']);
            updatedMatch.awayQualifies = this.parseOptionalQualifier(row['AwayQualifies']);
            updatedMatch.recordStatus = "Update";
  
            matches[matchIndex] = updatedMatch;
            updates++;
          }
        });
  
        this.matchesExtracted.emit(matches);
        this.showToast(this.t('TOASTS.ODDS_FILE_UPDATED', { count: updates }), 'success');
      } catch (error) {
        console.error('Error reading bets Excel file:', error);
        this.showToast(this.t('TOASTS.BETS_FILE_READ_FAILED'), 'danger');
      }
    };
  
    reader.readAsArrayBuffer(this.betsFile);
  }
  
  extractTeams(workbook: XLSX.WorkBook): Team[] {
    const sheet = workbook.Sheets['Teams'];
    const teams: Team[] = [];

    if (sheet) {
      const rows = XLSX.utils.sheet_to_json(sheet);
      rows.forEach((row: any) => {
        const teamName = row['Team Name'];
        if (typeof teamName === 'string' && teamName.trim()) {
          teams.push({
            teamFrontendId: this.generateFrontendId(), 
            teamId: null,
            predefinedTeamId: null,
            teamName: teamName.trim(),
            recordStatus: 'New'
          });
        }
      });

    } else {
      console.warn('No "Teams" sheet found in the Excel file.');
    }

    return teams;
  }

  extractStages(workbook: XLSX.WorkBook): Stage[] {
    const sheet = workbook.Sheets['Stages'];
    const stages: Stage[] = [];
  
    if (sheet) {
      const rows = XLSX.utils.sheet_to_json(sheet);
  
      rows.forEach((row: any, index: number) => {
        const stageName = typeof row['Stage Name'] === 'string' ? row['Stage Name'].trim() : '';
  
        if (stageName) {
          stages.push({
            stageFrontendId: this.generateFrontendId(),
            stageId: null,
            predefinedStageId: null,
            stageName: stageName,
            order: index + 1,
            recordStatus: 'New'
          });
        }
      });
  
    } else {
      console.warn('No "Stages" sheet found in the Excel file.');
    }
  
    return stages;
  }  
  
  extractMatches(workbook: XLSX.WorkBook, teams: Team[], stages: Stage[]): Match[] {
    const sheet = workbook.Sheets['Matches'];
    const matches: Match[] = [];
  
    if (sheet) {
      const rows = XLSX.utils.sheet_to_json(sheet);
  
      const teamMap = new Map(
        teams.map(team => [team.teamName.trim().toLowerCase(), { teamFrontendId: team.teamFrontendId, teamId: team.teamId }])
      );
      const stageMap = new Map(
        stages.map(stage => [stage.stageName.trim().toLowerCase(), { stageFrontendId: stage.stageFrontendId, stageId: stage.stageId, stageName: stage.stageName }])
      );
  
      rows.forEach((row: any) => {
        const homeTeamName = row['Home Team']?.trim();
        const awayTeamName = row['Away Team']?.trim();
        const stageName = row['Stage']?.trim()?.toLowerCase();
  
        const homeTeam = teamMap.get(homeTeamName?.toLowerCase());
        const awayTeam = teamMap.get(awayTeamName?.toLowerCase());
        const selectedStage = stageMap.get(stageName);
  
        if (!homeTeam || !awayTeam) {
          console.warn(`Unknown teams in match: ${homeTeamName} vs ${awayTeamName}`);
          return;
        }
  
        if (!selectedStage) {
          console.warn(`Unknown stage: ${row['Stage']}. Assigning default stage.`);
        }
  
        const match: Match = {
          matchFrontendId: this.generateFrontendId(),
          matchId: null,
          predefinedMatchId: null,
  
          stageFrontendId: selectedStage?.stageFrontendId ?? this.generateFrontendId(),
          stageId: selectedStage?.stageId ?? null,
          stageName: selectedStage?.stageName || row['Stage'] || 'Default Stage',
  
          homeTeamId: homeTeam.teamId,
          homeTeamFrontendId: homeTeam.teamFrontendId,
          homeTeam: homeTeamName,
  
          awayTeamId: awayTeam.teamId,
          awayTeamFrontendId: awayTeam.teamFrontendId,
          awayTeam: awayTeamName,
  
          matchStart: this.convertToTimestamp(row['Date'], row['Time'], row['Offset']),
          matchType: row['Match Type'] || 'default',
  
          homeWinOdds: this.parseMandatoryOdds(row['Home Win Odds'], 'Home Win Odds'),
          drawOdds: this.parseMandatoryOdds(row['Draw Odds'], 'Draw Odds'),
          awayWinOdds: this.parseMandatoryOdds(row['Away Win Odds'], 'Away Win Odds'),
        
          homeQualifies: this.parseOptionalQualifier(row['Home Team Qualifies']),
          awayQualifies: this.parseOptionalQualifier(row['Away Team Qualifies']),

          isVisible: true,

          recordStatus: 'New'
        };
  
        matches.push(match);
      });
    } else {
      console.warn('No "Matches" sheet found in the Excel file.');
    }
  
    return matches;
  }  

  private parseMandatoryOdds(value: any, label: string): number {
    const num = Number(value);
    if (isNaN(num) || num < 1) {
      throw new Error(`${label} must be a number greater than 1. Received: ${value}`);
    }
    return num;
  }
  
  private parseOptionalQualifier(value: any): number | null {
    if (value === undefined || value === '') return null;
    const num = Number(value);
    return !isNaN(num) && num >= 1 ? num : null;
  }
  

  convertToTimestamp(date: string, time: string, offset: number): string {
    if (!date || !time) return '';
    const dateTimeString = `${date}T${time}`;
    const localDate = new Date(dateTimeString);
    localDate.setHours(localDate.getHours() - (offset || 0));
    return localDate.toISOString();
  }

  parseOdds(odds: any): number {
    if (typeof odds === 'string') {
      const parsed = parseFloat(odds.replace(',', '.'));
      return isNaN(parsed) ? 0 : parsed;
    }
    return typeof odds === 'number' ? odds : 0;
  }

  async confirmEraseForm(): Promise<void> {
    const alert = await this.toastController.create({
      header: this.t('TOASTS.CONFIRM_ERASE_TITLE'),
      message: this.t('TOASTS.CONFIRM_ERASE_MESSAGE'),
      buttons: [
        { text: this.t('TOASTS.CANCEL'), role: 'cancel' },
        { text: this.t('TOASTS.ERASE'), role: 'destructive', handler: () => this.eraseForm() },
      ],
    });

    await alert.present();
  }

  eraseForm(): void {
    this.tournamentForm.reset();

    this.teamsExtracted.emit([]);
    this.stagesExtracted.emit([]);
    this.matchesExtracted.emit([]);

    if (this.isCustomTournamentCreateMode) {
      this.tournamentForm.patchValue({
        tournamentVisibility: 'Private',
        updateMethod: 'Manual',
        includeHomeAdvantage: true
      });
    }

    if (!this.isEditMode && this.isPredefinedTournament) {
      this.tournamentForm.patchValue({
        updateMethod: 'Manual',
        includeHomeAdvantage: true
      });
    }

    this.showToast(this.t('TOASTS.FORM_ERASED'), 'success');
  }

  private t(key: string, params?: Record<string, unknown>): string {
    return this.translate.instant(key, params);
  }

  private resolveTeamCrestUrl(team: Team): string | null {
    const externalTeam = team as Team & { crest?: string | null };
    return team.crestUrl ?? externalTeam.crest ?? null;
  }
  
  generateFrontendId(): string {
    return 'F-' + Math.random().toString(36).substr(2, 9);
  }
}
