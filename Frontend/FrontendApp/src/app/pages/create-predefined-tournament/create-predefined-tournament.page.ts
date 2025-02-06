import { Component } from '@angular/core';
import * as XLSX from 'xlsx';
import { FormBuilder, FormGroup, Validators, AbstractControl, ReactiveFormsModule, FormArray, FormControl } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { IonicModule, ToastController } from '@ionic/angular';
import { ModalController } from '@ionic/angular';
import { EditMatchModalComponent } from '..//../modals/edit-match-modal/edit-match-modal.component'; // Import the modal

@Component({
  selector: 'app-create-predefined-tournament',
  templateUrl: './create-predefined-tournament.page.html',
  styleUrls: ['./create-predefined-tournament.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule],
})
export class CreatePredefinedTournamentPage {
  step = 1;
  file: File | null = null;
  betForm: FormGroup;
  teamMap: { [key: number]: string } = {}; // Holds the mapping of Team ID → Team Name

  constructor(private fb: FormBuilder, private toastController: ToastController, private modalController: ModalController, ) {
    this.betForm = this.fb.group({
      tournamentName: ['', [Validators.required, Validators.maxLength(50)]],
      uploadType: new FormControl('manual'),
      teams: this.fb.array([], Validators.required),
      matches: this.fb.array([]),
    });
  }

  // Getters for FormArray
  get teamsArray(): FormArray {
    return this.betForm.get('teams') as FormArray;
  }

  get matchesArray(): FormArray {
    return this.betForm.get('matches') as FormArray;
  }

  get uploadType(): FormControl {
    return this.betForm.get('uploadType') as FormControl;
  }

  get tournamentName(): FormControl {
    return this.betForm.get('tournamentName') as FormControl;
  }

  getTeamControl(index: number): FormControl {
    return this.teamsArray.at(index) as FormControl;
  }

  getMatchControl(index: number): FormGroup {
    return this.matchesArray.at(index) as FormGroup;
  }

  nextStep() {
    if (this.step < 4) this.step++;
  }

  prevStep() {
    if (this.step > 1) this.step--;
  }

  handleFileInput(event: any) {
    this.file = event.target.files[0];
    if (this.file) this.readExcelFile();
  }

  async openEditModal(index?: number) {
    const matchData = index !== undefined ? this.getMatchControl(index).value : {
      matchId: null,
      stage: '',
      homeTeamId: null,
      homeTeam: '',
      awayTeamId: null,
      awayTeam: '',
      date: '',
      betType: '',
      homeWinOdds: '',
      drawOdds: '',
      awayWinOdds: '',
      homeQualifies: '',
      awayQualifies: ''
    };
  
    const modal = await this.modalController.create({
      component: EditMatchModalComponent,
      componentProps: {
        match: matchData, // Pass match details (empty if new)
        index: index,
        teams: this.teamsArray.value, // Pass full list of teams
      }
    });
  
    modal.onDidDismiss().then((result) => {
      if (result.data) {
        if (index !== undefined) {
          this.matchesArray.at(index).setValue(result.data);
        } else {
          const newMatchGroup = this.fb.group({ ...result.data });
          this.matchesArray.push(newMatchGroup);
          this.matchesArray.updateValueAndValidity();
        }
      }
    });
      
    return await modal.present();
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
  
        this.extractTeams(workbook); // Ensure teams are extracted first
        setTimeout(() => { // Small delay to ensure teamMap is populated
          this.extractMatches(workbook);
        }, 100);
  
        this.showToast('Excel file read successfully!', 'success');
      } catch (error) {
        this.showToast('Error reading Excel file!', 'danger');
        console.error('Excel read error:', error);
      }
    };
    reader.readAsArrayBuffer(this.file!);
  }
  
  extractTeams(workbook: XLSX.WorkBook) {
    const sheet = workbook.Sheets['Teams'];
    if (sheet) {
      const rows = XLSX.utils.sheet_to_json(sheet);
      this.teamsArray.clear();
      this.teamMap = {}; // Reset map
  
      rows.forEach((row: any) => {
        const teamId = Number(row['Team ID']); // Ensure it's a number
        const teamName = row['Team Name'];
  
        if (!isNaN(teamId) && typeof teamName === 'string' && teamName.trim()) {
          this.teamsArray.push(new FormControl(teamName.trim(), Validators.required));
          this.teamMap[teamId] = teamName.trim(); // Store mapping
        }
      });
  
      console.log('Final Team Map:', this.teamMap); // Debugging log
    }
  }
  
  extractMatches(workbook: XLSX.WorkBook) {
    const sheet = workbook.Sheets['Games'];
    if (sheet) {
      const rows = XLSX.utils.sheet_to_json(sheet);
      this.matchesArray.clear();
  
      console.log('Using Team Map:', this.teamMap); // Debug log
  
      rows.forEach((row: any) => {
        const homeTeamId = Number(row['Home Team ID']);
        const awayTeamId = Number(row['Away Team ID']);
  
        console.log(`Processing match: ${homeTeamId} vs ${awayTeamId}`); // Debug log
  
        if (!this.teamMap[homeTeamId] || !this.teamMap[awayTeamId]) {
          console.warn(`Missing team for match: ${homeTeamId} vs ${awayTeamId}`);
        }
  
        if (typeof homeTeamId === 'number' && typeof awayTeamId === 'number') {
          const matchDateTime = this.convertToTimestamp(row['Date'], row['Time'], row['UTC Offset']);
  
          // Get team names from the map using IDs
          const homeTeamName = this.teamMap[homeTeamId] || `Unknown Team (${homeTeamId})`;
          const awayTeamName = this.teamMap[awayTeamId] || `Unknown Team (${awayTeamId})`;
  
          console.log(`Match: ${homeTeamName} vs ${awayTeamName}`); // Debug log
  
          this.matchesArray.push(this.fb.group({
            matchId: [row['Match ID'], Validators.required],
            stage: [row['Stage'], Validators.required],
            homeTeamId: [homeTeamId, Validators.required],
            homeTeam: [homeTeamName, Validators.required], // Now correctly mapped
            awayTeamId: [awayTeamId, Validators.required],
            awayTeam: [awayTeamName, Validators.required], // Now correctly mapped
            date: [matchDateTime, Validators.required],
            betType: [row['Bet Type'], Validators.required],
            homeWinOdds: [this.parseOdds(row['Home Win Odds']), Validators.required],
            drawOdds: [this.parseOdds(row['Draw Odds']), Validators.required],
            awayWinOdds: [this.parseOdds(row['Away Win Odds']), Validators.required],
            homeQualifies: [this.parseOdds(row['Home Team Qualifies']), Validators.required],
            awayQualifies: [this.parseOdds(row['Away Team Qualifies']), Validators.required],
          }));
        }
      });
    }
  }
  
  convertToTimestamp(date: string, time: string, utcOffset: number): string {
    const dateTimeString = `${date} ${time}`;
    const localDate = new Date(dateTimeString);

    // Adjust for UTC offset
    localDate.setHours(localDate.getHours() - utcOffset);

    return localDate.toISOString(); // Store as ISO timestamp
  }

  parseOdds(odds: any): number {
    if (typeof odds === 'string') {
      return parseFloat(odds.replace(',', '.')); // Replace comma with dot
    }
    return typeof odds === 'number' ? odds : NaN;
  }

  async addTeam(inputRef: any) {
    const inputElement = await inputRef.getInputElement(); // Get native input element
    const teamName = inputElement.value.trim(); // Extract value properly
  
    if (!teamName) {
      this.showToast('Team name cannot be empty!', 'warning');
      return;
    }
  
    if (teamName.length > 50) {
      this.showToast('Team name cannot exceed 50 characters!', 'warning');
      return;
    }
  
    const existingNames = this.teamsArray.value.map((team: string) => team.toLowerCase());
    if (existingNames.includes(teamName.toLowerCase())) {
      this.showToast('Team already exists!', 'danger');
      return;
    }
  
    this.teamsArray.push(new FormControl(teamName, Validators.required));
    this.showToast(`Added team: ${teamName}`, 'success');
  
    inputElement.value = '';
  }
  
  removeTeam(index: number) {
    const teamName = this.teamsArray.at(index).value;
    this.teamsArray.removeAt(index);
    this.showToast(`Removed team: ${teamName}`, 'success');
  }
  
  addMatch(home: any, away: any, date: any) {
    if (typeof home === 'string' && typeof away === 'string' && typeof date === 'string') {
      this.matchesArray.push(this.fb.group({ home, away, date }));
    }
  }

  removeMatch(index: number) {
    this.matchesArray.removeAt(index);
  }

  submitData() {
    console.log('Submitting Data:', this.betForm.value);
    // Send data to backend
  }
}
