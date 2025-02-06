import { Component } from '@angular/core';
import * as XLSX from 'xlsx';
import { FormBuilder, FormGroup, Validators, AbstractControl, ReactiveFormsModule, FormArray, FormControl } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { IonicModule, ToastController } from '@ionic/angular';

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

  constructor(private fb: FormBuilder) {
    this.betForm = this.fb.group({
      uploadType: new FormControl('manual'),
      teams: this.fb.array([], Validators.required),
      matches: this.fb.array([])
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

  readExcelFile() {
    const reader = new FileReader();
    reader.onload = (e: any) => {
      const data = new Uint8Array(e.target.result);
      const workbook = XLSX.read(data, { type: 'array' });
      this.extractTeams(workbook);
      this.extractMatches(workbook);
    };
    reader.readAsArrayBuffer(this.file!);
  }

  extractTeams(workbook: XLSX.WorkBook) {
    const sheet = workbook.Sheets['Teams'];
    if (sheet) {
      const rows = XLSX.utils.sheet_to_json(sheet);
      this.teamsArray.clear();
      rows.forEach((row: any) => {
        const teamName = row['Team Name'];
        if (typeof teamName === 'string') {
          this.teamsArray.push(new FormControl(teamName.trim(), Validators.required));
        }
      });
    }
  }

  extractMatches(workbook: XLSX.WorkBook) {
    const sheet = workbook.Sheets['Games'];
    if (sheet) {
      const rows = XLSX.utils.sheet_to_json(sheet);
      this.matchesArray.clear();
      rows.forEach((row: any) => {
        if (typeof row['Home Team'] === 'string' && typeof row['Away Team'] === 'string') {
          this.matchesArray.push(this.fb.group({
            home: [row['Home Team'].trim(), Validators.required],
            away: [row['Away Team'].trim(), Validators.required],
            date: [row['Date'], Validators.required]
          }));
        }
      });
    }
  }

  addTeam(teamName: any) {
    if (typeof teamName === 'string' && teamName.trim()) {
      this.teamsArray.push(new FormControl(teamName.trim(), Validators.required));
    }
  }

  removeTeam(index: number) {
    this.teamsArray.removeAt(index);
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
