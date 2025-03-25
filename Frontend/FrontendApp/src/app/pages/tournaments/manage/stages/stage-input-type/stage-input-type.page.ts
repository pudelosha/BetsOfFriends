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

@Component({
  selector: 'app-stage-input-type',
  templateUrl: './stage-input-type.page.html',
  styleUrls: ['./stage-input-type.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule, FormsModule],
})
export class StageInputTypePage implements OnInit {
  @Input() tournamentForm!: FormGroup;
  @Input() isEditMode: boolean = false;
  @Input() isPredefinedTournament: boolean = false;
  @Output() teamsExtracted = new EventEmitter<Team[]>(); 
  @Output() stagesExtracted = new EventEmitter<Stage[]>();
  @Output() matchesExtracted = new EventEmitter<Match[]>(); 
  @Output() tournamentSelected = new EventEmitter<Tournament>();
  @ViewChild('fileInput') fileInput!: ElementRef;

  file: File | null = null;
  predefinedTournaments: Tournament[] = [];
  selectedTournamentId: number | null = null;

  constructor(
    private toastController: ToastController, 
    private tournamentService: PredefinedTournamentService, 
    private modalController: ModalController
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

          // Ensure all entities are marked as "Uploaded"
          tournament.teams = tournament.teams.map(team => ({ ...team, recordStatus: 'Uploaded' }));
          tournament.stages = tournament.stages.map(stage => ({ ...stage, recordStatus: 'Uploaded' }));
          tournament.matches = tournament.matches.map(match => ({ ...match, recordStatus: 'Uploaded' }));
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
        this.matchesExtracted.emit(matches);
        this.stagesExtracted.emit(stages);

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
  
          homeWinOdds: this.parseOdds(row['Home Win Odds']) ?? 0,
          drawOdds: this.parseOdds(row['Draw Odds']) ?? 0,
          awayWinOdds: this.parseOdds(row['Away Win Odds']) ?? 0,
  
          homeQualifies: null,
          awayQualifies: null,

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
