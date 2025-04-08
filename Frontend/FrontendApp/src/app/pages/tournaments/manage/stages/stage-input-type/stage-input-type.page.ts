import { Component, Input, Output, EventEmitter, ViewChild, ElementRef, OnInit } from '@angular/core';
import * as XLSX from 'xlsx';
import { FormGroup, ReactiveFormsModule, FormArray } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { IonicModule, ToastController } from '@ionic/angular';
import { FormsModule } from '@angular/forms'; // Import FormsModule
import { Tournament, Team, Match, Stage } from 'src/app/model/tournament-model';
import { PredefinedTournamentService } from 'src/app/services/predefined-tournament.service';
import { ModalController } from '@ionic/angular';
import { TournamentSelectionModalComponent } from 'src/app/modals/tournament-selection-modal/tournament-selection.modal';
import { TranslateModule } from '@ngx-translate/core';
import { AlertController } from '@ionic/angular';
import { SelectCompetitionModalComponent } from 'src/app/modals/select-competition-modal/select-competition-modal.component';
import { ExternalDataService } from 'src/app/services/external-data.service';


@Component({
  selector: 'app-stage-input-type',
  templateUrl: './stage-input-type.page.html',
  styleUrls: ['./stage-input-type.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule, FormsModule, TranslateModule],
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

  file: File | null = null;
  predefinedTournaments: Tournament[] = [];
  selectedTournamentId: number | null = null;
  isLoading: boolean = false;

  constructor(
    private toastController: ToastController, 
    private tournamentService: PredefinedTournamentService, 
    private modalController: ModalController,
    private alertController: AlertController,
    private externalDataService: ExternalDataService
  ) {}

  ngOnInit(): void {
    console.log('[StageInputType] ngOnInit');
    console.log('isEditMode:', this.isEditMode);
    console.log('isPredefinedTournament:', this.isPredefinedTournament);
    console.log('isCustomTournamentCreateMode:', this.isCustomTournamentCreateMode);
    console.log('isCustomTournamentEditMode:', this.isCustomTournamentEditMode);
  
    if (this.isCustomTournamentCreateMode) {
      console.log('Setting defaults for Custom - Create');
      this.tournamentForm.patchValue({
        tournamentVisibility: 'Private',
        updateMethod: 'Manual',
      });
    } else if (!this.isEditMode && this.isPredefinedTournament) {
      console.log('Setting defaults for Predefined - Create');
      this.tournamentForm.patchValue({
        updateMethod: 'Manual',
      });
    }
  
    if (this.isCustomTournamentCreateMode) {
      console.log('Loading predefined tournaments...');
      this.loadPredefinedTournaments();
    }
  }
          
  loadPredefinedTournaments(): void {
    this.tournamentService.getActivePredefinedTournaments().subscribe({
      next: (tournaments) => {
        this.predefinedTournaments = tournaments;
        console.log('Loaded predefined tournaments:', tournaments);
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
      this.tournamentService.getPredefinedTournamentById(this.selectedTournamentId).subscribe({
        next: (tournament) => {
          console.log('Fetched Tournament:', tournament);

          // set tournamentId as predefinedTournamentId
          tournament.predefinedTournamentId = tournament?.tournamentId ?? null;

          // Ensure all entities are marked as "Uploaded"
          tournament.teams = tournament.teams.map(team => ({
            ...team,
            recordStatus: 'Uploaded',
            predefinedTeamId: team.teamId,
          }));
          
          tournament.stages = tournament.stages.map(stage => ({
            ...stage,
            recordStatus: 'Uploaded',
            predefinedStageId: stage.stageId,
          }));
          
          tournament.matches = tournament.matches.map(match => ({
            ...match,
            recordStatus: 'Uploaded',
            predefinedMatchId: match.matchId,
          }));
          
          tournament.users = tournament.users
            ? tournament.users.map(user => ({ ...user, recordStatus: 'Uploaded' }))
            : []; // Default to an empty array if null

          this.tournamentSelected.emit(tournament);
          this.showToast('Tournament loaded successfully!', 'success');
        },
        error: (err) => {
          console.error('Error fetching tournament:', err);
          this.showToast('Failed to load tournament data!', 'danger');
        },
      });
    } else {
      console.error('Selected Tournament ID is null.');
      this.showToast('No tournament selected!', 'warning');
    }
  }

  handleFileInput(event: any) {
    this.file = event.target.files[0];
    if (this.file) {
      this.readExcelFile();
    }
  }

  triggerFileInput(): void {
    this.fileInput.nativeElement.click();
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
  
      console.log(`Importing matches for competition ${competitionCode}, season ${seasonCode}`);
  
      this.isLoading = true;
  
      this.externalDataService.getCompetitionMatches(competitionCode, seasonCode).subscribe({
        next: (tournament) => {
          console.log('Fetched Tournament DTO:', tournament);
  
          const teams: Team[] = tournament.teams.map(t => ({
            teamFrontendId: this.generateFrontendId(),
            teamId: null,
            externalTeamId: t.externalTeamId,
            predefinedTeamId: t.teamId,
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
  
          this.showToast('Competition matches loaded successfully!', 'success');
        },
        error: (err) => {
          console.error('Error fetching competition matches:', err);
          this.showToast('Failed to load competition data.', 'danger');
        },
        complete: () => {
          this.isLoading = false;
        }
      });
  
    } else {
      this.showToast('No competition selected.', 'warning');
    }
  }  
  
  async triggerTournamentUpdate(): Promise<void> {
    const alert = await this.alertController.create({
      header: 'Confirm Update',
      message: 'This will refresh your tournament with the latest data from the source. All matching records will be updated and marked accordingly.',
      buttons: [
        {
          text: 'Cancel',
          role: 'cancel',
        },
        {
          text: 'Update',
          handler: () => {
            this.tournamentUpdateRequested.emit();
          },
        },
      ],
    });
  
    await alert.present();
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

        this.showToast('Excel file read successfully!', 'success');
      } catch (error) {
        this.showToast('Error reading Excel file!', 'danger');
        console.error('Excel read error:', error);
      }
    };

    reader.readAsArrayBuffer(this.file!);
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
            recordStatus: 'New' // Mark imported teams as "New"
          });
        }
      });

      console.log('Extracted Teams:', teams);
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
            order: index + 1, // Assigns order based on position in the list
            recordStatus: 'New' // Mark imported stages as "New"
          });
        }
      });
  
      console.log('Extracted Stages:', stages);
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
  
      // Create lookup maps for teams and stages
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
  
          stageFrontendId: selectedStage?.stageFrontendId ?? this.generateFrontendId(), // Ensure non-null string
          stageId: selectedStage?.stageId ?? null,
          stageName: selectedStage?.stageName || row['Stage'] || 'Default Stage',
  
          homeTeamId: homeTeam.teamId,
          homeTeamFrontendId: homeTeam.teamFrontendId,
          homeTeam: homeTeamName,
  
          awayTeamId: awayTeam.teamId,
          awayTeamFrontendId: awayTeam.teamFrontendId,
          awayTeam: awayTeamName,
  
          matchStart: this.convertToTimestamp(row['Date'], row['Time'], row['UTC Offset']),
          matchType: row['Match Type'] || 'default',
  
          homeWinOdds: this.parseMandatoryOdds(row['Home Win Odds'], 'Home Win Odds'),
          drawOdds: this.parseMandatoryOdds(row['Draw Odds'], 'Draw Odds'),
          awayWinOdds: this.parseMandatoryOdds(row['Away Win Odds'], 'Away Win Odds'),
        
          homeQualifies: this.parseOptionalQualifier(row['Home Team Qualifies']),
          awayQualifies: this.parseOptionalQualifier(row['Away Team Qualifies']),

          isVisible: true,

          recordStatus: 'New' // Mark imported matches as "New"
        };
  
        matches.push(match);
      });
  
      console.log('Extracted Matches:', matches);
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
  

  convertToTimestamp(date: string, time: string, utcOffset: number): string {
    if (!date || !time) return '';
    const dateTimeString = `${date}T${time}`;
    const localDate = new Date(dateTimeString);
    localDate.setHours(localDate.getHours() - (utcOffset || 0));
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
      header: 'Confirm Erase',
      message: 'Are you sure you want to erase all data from the form? This action cannot be undone.',
      buttons: [
        { text: 'Cancel', role: 'cancel' },
        { text: 'Erase', role: 'destructive', handler: () => this.eraseForm() },
      ],
    });

    await alert.present();
  }

  eraseForm(): void {
    this.tournamentForm.reset();
  
    // Emit empty lists to parent to clear teams, stages, matches
    this.teamsExtracted.emit([]);
    this.stagesExtracted.emit([]);
    this.matchesExtracted.emit([]);
  
    // Reapply default values based on mode
    if (this.isCustomTournamentCreateMode) {
      this.tournamentForm.patchValue({
        tournamentVisibility: 'Private',
        updateMethod: 'Manual',
      });
    }
  
    if (!this.isEditMode && this.isPredefinedTournament) {
      this.tournamentForm.patchValue({
        updateMethod: 'Manual',
      });
    }
  
    this.showToast('All form data has been erased.', 'success');
  }
  
  generateFrontendId(): string {
    return 'F-' + Math.random().toString(36).substr(2, 9);
  }
}
