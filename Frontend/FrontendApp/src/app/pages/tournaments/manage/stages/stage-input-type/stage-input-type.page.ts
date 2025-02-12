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

@Component({
  selector: 'app-stage-input-type',
  templateUrl: './stage-input-type.page.html',
  styleUrls: ['./stage-input-type.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule, FormsModule],
})
export class StageInputTypePage implements OnInit {
  @Input() showPredefinedImport: boolean = false;
  @Input() tournamentForm!: FormGroup; // Parent form
  @Output() teamsExtracted = new EventEmitter<any[]>(); // Emit teams to parent
  @Output() matchesExtracted = new EventEmitter<any[]>(); // Emit matches to parent
  @ViewChild('fileInput') fileInput!: ElementRef;

  file: File | null = null;
  importFileMethod: string = 'upload'; // Default value
  importTournamentMethod: string = 'upload';
  predefinedTournaments: Tournament[] = []; // Holds active predefined tournaments
  selectedTournamentId: number | null = null; // Holds the selected tournament ID

  constructor(private toastController: ToastController, private tournamentService: PredefinedTournamentService, private modalController: ModalController) {}

  ngOnInit(): void {
    this.importFileMethod = 'upload';
    this.importTournamentMethod = 'upload';

    if (this.showPredefinedImport) {
      this.loadPredefinedTournaments();
    }
  }

  onFileExtractChange(event: CustomEvent): void {
    const method = event.detail.value;
    this.tournamentForm.get('importFileMode')?.setValue(method);
    console.log('Import File Mode:', method);
  }
  
  onTournamentExtractChange(event: CustomEvent): void {
    const method = event.detail.value;
    this.tournamentForm.get('importTournamentMode')?.setValue(method);
    console.log('Import Tournament Mode:', method);
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
    if (data?.selectedTournamentId !== null) {
      this.selectedTournamentId = data.selectedTournamentId;
      console.log('Selected Tournament ID:', this.selectedTournamentId);
      // Handle the selected tournament (upload or append logic)
      // TODO
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
  
        // Extract teams and matches
        const teams = this.extractTeams(workbook);
        const matches = this.extractMatches(workbook, teams.map(t => t.teamName)); // Pass only team names to match extraction
  
        // Emit data to parent
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
  
  extractTeams(workbook: XLSX.WorkBook): { teamId: null; teamName: string }[] {
    const sheet = workbook.Sheets['Teams'];
    const teams: any[] = [];
  
    if (sheet) {
      const rows = XLSX.utils.sheet_to_json(sheet);
      rows.forEach((row: any) => {
        const teamName = row['Team Name'];
        if (typeof teamName === 'string' && teamName.trim()) {
          teams.push({
            teamId: null, // All new teams have teamId set to null
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

  extractMatches(workbook: XLSX.WorkBook, teamNames: string[]): any[] {
    const sheet = workbook.Sheets['Games'];
    const matches: any[] = [];
  
    if (sheet) {
      const rows = XLSX.utils.sheet_to_json(sheet);
  
      rows.forEach((row: any) => {
        const homeTeam = row['Home Team'];
        const awayTeam = row['Away Team'];
  
        if (!teamNames.includes(homeTeam) || !teamNames.includes(awayTeam)) {
          console.warn(`Unknown teams in match: ${homeTeam} vs ${awayTeam}`);
          return;
        }
  
        const match = {
          stage: row['Stage'] || null,
          homeTeam,
          awayTeam,
          matchStart: this.convertToTimestamp(row['Date'], row['Time'], row['UTC Offset']),
          homeWinOdds: this.parseOdds(row['Home Win Odds']),
          drawOdds: this.parseOdds(row['Draw Odds']),
          awayWinOdds: this.parseOdds(row['Away Win Odds']),
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
    this.tournamentForm.get('importFileMode')?.setValue('upload');
    this.tournamentForm.get('importTournamentMode')?.setValue('upload');
  
    this.importFileMethod = 'upload';
    this.importTournamentMethod = 'upload';
  
    this.teamsExtracted.emit([]);
    this.matchesExtracted.emit([]);
  
    this.showToast('All form data has been erased.', 'success');
  } 
}