import { Component, Input, Output, EventEmitter, ViewChild, ElementRef, OnInit } from '@angular/core';
import * as XLSX from 'xlsx';
import { FormGroup, ReactiveFormsModule, FormArray } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { IonicModule, ToastController } from '@ionic/angular';
import { FormsModule } from '@angular/forms'; // Import FormsModule
import { Tournament } from 'src/app/model/tournament-model';
import { PredefinedTournamentService } from 'src/app/services/predefined-tournament.service';
import { ModalController } from '@ionic/angular';
import { TournamentSelectionModalComponent } from 'src/app/modals/tournament-selection-modal/tournament-selection.modal';
import { Team, Match } from 'src/app/model/tournament-model';

@Component({
  selector: 'app-stage-input-type',
  templateUrl: './stage-input-type.page.html',
  styleUrls: ['./stage-input-type.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule, FormsModule],
})
export class StageInputTypePage implements OnInit {
  @Input() showPredefinedImport: boolean = false;
  @Input() tournamentForm!: FormGroup;
  @Output() teamsExtracted = new EventEmitter<Team[]>(); 
  @Output() matchesExtracted = new EventEmitter<Match[]>(); 
  @Output() tournamentSelected = new EventEmitter<Tournament>();
  @ViewChild('fileInput') fileInput!: ElementRef;

  file: File | null = null;
  importMethod: string = 'upload'; // Default value
  predefinedTournaments: Tournament[] = []; // Holds active predefined tournaments
  selectedTournamentId: number | null = null; // Holds the selected tournament ID

  constructor(private toastController: ToastController, private tournamentService: PredefinedTournamentService, private modalController: ModalController) {}

  ngOnInit(): void {
    this.importMethod = 'upload';

    if (this.showPredefinedImport) {
      this.loadPredefinedTournaments();
    }
  }

  onImportMethodChange(event: CustomEvent): void {
    const method = event.detail.value;
    this.tournamentForm.get('importMethod')?.setValue(method);
    console.log('Import Method:', method);
  }
  
  private loadPredefinedTournaments(): void {
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

  async openTournamentSelection(): Promise<void> {
    const modal = await this.modalController.create({
      component: TournamentSelectionModalComponent,
      componentProps: {
        predefinedTournaments: this.predefinedTournaments,
      },
      breakpoints: [0, 0.5, 1],
      initialBreakpoint: 0.5, // Popup height
    });
  
    await modal.present();
  
    const { data } = await modal.onWillDismiss();
    
    // Assign the selected tournament ID from the modal data
    this.selectedTournamentId = data?.selectedTournamentId ?? null;
  
    if (this.selectedTournamentId !== null) {
      this.tournamentService.getPredefinedTournamentById(this.selectedTournamentId).subscribe({
        next: (tournament: Tournament) => {
          console.log('Fetched Tournament:', tournament);
          
          // Emit the fetched tournament data to the parent
          this.tournamentSelected.emit(tournament);
  
          // Show a success toast
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
  
        // Extract structured teams and matches
        const teams = this.extractTeams(workbook);
        const matches = this.extractMatches(workbook, teams);
  
        // Emit structured data to the parent component
        this.teamsExtracted.emit(teams);
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
            frontendId: this.generateFrontendId(), // Generate unique frontend ID
            backendId: null, // All new teams initially have null backendId
            teamName: teamName.trim(),
          });
        }
      });
  
      console.log('Extracted Teams:', teams);
    } else {
      console.warn('No "Teams" sheet found in the Excel file.');
    }
  
    return teams;
  }
   
  extractMatches(workbook: XLSX.WorkBook, teams: Team[]): Match[] {
    const sheet = workbook.Sheets['Games'];
    const matches: Match[] = [];
  
    if (sheet) {
      const rows = XLSX.utils.sheet_to_json(sheet);
  
      // Create a map of team names to frontendId and backendId for reference
      const teamMap = new Map(
        teams.map(team => [
          team.teamName, 
          { frontendId: team.frontendId, backendId: team.backendId }
        ])
      );
  
      rows.forEach((row: any) => {
        const homeTeamName = row['Home Team'];
        const awayTeamName = row['Away Team'];
  
        // Ensure both teams exist
        const homeTeam = teamMap.get(homeTeamName);
        const awayTeam = teamMap.get(awayTeamName);
  
        if (!homeTeam || !awayTeam) {
          console.warn(`Unknown teams in match: ${homeTeamName} vs ${awayTeamName}`);
          return; // Skip invalid matches
        }
  
        const match: Match = {
          frontendId: this.generateFrontendId(), // Unique ID for frontend tracking
          backendId: null, // Initially null for new matches
  
          stage: row['Stage'] || null,
  
          homeTeamId: homeTeam.backendId, // Backend ID if available, otherwise null
          homeTeamFrontendId: homeTeam.frontendId, // Always present
          homeTeam: homeTeamName,
  
          awayTeamId: awayTeam.backendId, // Backend ID if available, otherwise null
          awayTeamFrontendId: awayTeam.frontendId, // Always present
          awayTeam: awayTeamName,
  
          matchStart: this.convertToTimestamp(row['Date'], row['Time'], row['UTC Offset']),
  
          betType: row['Bet Type'] || 'default', // Assign a default value if missing
  
          homeWinOdds: this.parseOdds(row['Home Win Odds']) ?? 0, // Ensure a number
          drawOdds: this.parseOdds(row['Draw Odds']) ?? 0,
          awayWinOdds: this.parseOdds(row['Away Win Odds']) ?? 0,
  
          homeQualifies: null, // Default for now, can be set later
          awayQualifies: null,
        };
  
        matches.push(match);
      });
  
      console.log('Extracted Matches:', matches);
    } else {
      console.warn('No "Games" sheet found in the Excel file.');
    }
  
    return matches;
  }
        
  convertToTimestamp(date: string, time: string, utcOffset: number): string {
    if (!date || !time) return '';
    const dateTimeString = `${date}T${time}`;
    const localDate = new Date(dateTimeString);
    localDate.setHours(localDate.getHours() - (utcOffset || 0)); // Apply UTC offset if provided
    return localDate.toISOString();
  }

  parseOdds(odds: any): number | null {
    if (typeof odds === 'string') {
      const parsed = parseFloat(odds.replace(',', '.'));
      return isNaN(parsed) ? null : parsed;
    }
    return typeof odds === 'number' ? odds : null;
  }

  async confirmEraseForm(): Promise<void> {
    const alert = await this.toastController.create({
      header: 'Confirm Erase',
      message: 'Are you sure you want to erase all data from the form? This action cannot be undone.',
      buttons: [
        {
          text: 'Cancel',
          role: 'cancel',
        },
        {
          text: 'Erase',
          role: 'destructive',
          handler: () => this.eraseForm(),
        },
      ],
    });
  
    await alert.present();
  }
  
  eraseForm(): void {
    const teamsArray = this.tournamentForm.get('teams') as FormArray;
    const matchesArray = this.tournamentForm.get('matches') as FormArray;
  
    if (teamsArray) teamsArray.clear();
    if (matchesArray) matchesArray.clear();
  
    this.tournamentForm.get('tournamentName')?.setValue('');
    this.tournamentForm.get('importMethod')?.setValue('upload');
  
    this.importMethod = 'upload';
  
    this.teamsExtracted.emit([]);
    this.matchesExtracted.emit([]);
  
    this.showToast('All form data has been erased.', 'success');
  } 

  generateFrontendId(): string {
    return 'F-' + Math.random().toString(36).substr(2, 9); // Simple unique ID
  }  
}

