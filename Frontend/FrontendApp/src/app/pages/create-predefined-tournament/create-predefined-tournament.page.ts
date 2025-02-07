import { Component, OnInit } from '@angular/core';
import * as XLSX from 'xlsx';
import { ActivatedRoute } from '@angular/router';
import { FormBuilder, FormGroup, Validators, AbstractControl, ReactiveFormsModule, FormArray, FormControl } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { IonicModule, ToastController, AlertController } from '@ionic/angular';
import { ModalController, ViewWillEnter } from '@ionic/angular';
import { EditMatchModalComponent } from '..//../modals/edit-match-modal/edit-match-modal.component';
import { Tournament, Team, Match } from '../../model/tournament-model';
import { PredefinedTournamentService } from '../../services/predefined-tournament.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-create-predefined-tournament',
  templateUrl: './create-predefined-tournament.page.html',
  styleUrls: ['./create-predefined-tournament.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule],
})
export class CreatePredefinedTournamentPage implements OnInit, ViewWillEnter  {
  tournamentId?: number; // Optional tournament ID
  step = 1;
  tournamentForm: FormGroup;
  file: File | null = null;
  teamMap: { [key: number]: string } = {}; // Holds the mapping of Team ID → Team Name

  constructor(private fb: FormBuilder, 
    private toastController: ToastController, 
    private modalController: ModalController, 
    private alertController: AlertController,
    private route: ActivatedRoute,
    private tournamentService: PredefinedTournamentService,
    private router: Router
  ) {
    this.tournamentForm = this.fb.group({
      tournamentName: ['', [Validators.required, Validators.maxLength(50)]],
      createdBy: [''],
      createdAt: [''],
      teams: this.fb.array([], Validators.required),
      matches: this.fb.array([]),
    });
  }

  ionViewWillEnter() {
    this.resetForm(); // Reset form when user navigates back
  }

  ngOnInit() {
    this.resetForm(); // Reset form when the page is initialized
    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) {
        this.tournamentId = +id;
        this.loadTournamentData(this.tournamentId);
      }
    });
  }

  private loadTournamentData(tournamentId: number) {
    this.tournamentService.getPredefinedTournamentById(tournamentId).subscribe(tournament => {
      if (!tournament) {
        this.showToast('Tournament not found!', 'danger');
        return;
      }

      this.tournamentForm.patchValue({
        tournamentName: tournament.tournamentName,
        createdBy: tournament.createdBy,
        createdAt: tournament.createdAt,
      });

      // Load teams
      const teamsArray = this.tournamentForm.get('teams') as FormArray;
      tournament.teams.forEach(team => {
        teamsArray.push(new FormControl(team.teamName, Validators.required));
      });

      // Load matches
      const matchesArray = this.tournamentForm.get('matches') as FormArray;
      tournament.matches.forEach(match => {
        matchesArray.push(this.fb.group({
          matchId: [match.matchId],
          stage: [match.stage],
          homeTeamId: [match.homeTeamId],
          homeTeam: [match.homeTeam, Validators.required],
          awayTeamId: [match.awayTeamId],
          awayTeam: [match.awayTeam, Validators.required],
          matchStart: [match.matchStart, Validators.required],
          betType: [match.betType, Validators.required],
          homeWinOdds: [match.homeWinOdds, Validators.required],
          drawOdds: [match.drawOdds, Validators.required],
          awayWinOdds: [match.awayWinOdds, Validators.required],
          homeQualifies: [match.homeQualifies, Validators.required],
          awayQualifies: [match.awayQualifies, Validators.required],
        }));
      });
    });
  }

  // Getters for FormArray
  get teamsArray(): FormArray {
    return this.tournamentForm.get('teams') as FormArray;
  }

  get matchesArray(): FormArray {
    return this.tournamentForm.get('matches') as FormArray;
  }

  get uploadType(): FormControl {
    return this.tournamentForm.get('uploadType') as FormControl;
  }

  get tournamentName(): FormControl {
    return this.tournamentForm.get('tournamentName') as FormControl;
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
    const existingMatch = index !== undefined ? this.getMatchControl(index).value : null;
  
    const matchData = existingMatch || {
      matchId: this.generateMatchId(), // Generate only for new match
      stage: '', // Optional, removed required validator
      homeTeamId: null, // These will be set dynamically
      homeTeam: '',
      awayTeamId: null,
      awayTeam: '',
      matchStart: new Date().toISOString(), // Ensure a valid default timestamp
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
      console.log(" Modal dismissed, result:", result.data);
    
      if (result.data) {
        // First, ensure teams array is populated
        console.log(" Current Teams Array:", this.teamsArray.value);
    
        // Fetch team IDs dynamically
        const homeTeamId = this.findTeamIdByName(result.data.homeTeam);
        const awayTeamId = this.findTeamIdByName(result.data.awayTeam);
    
        // Debugging logs to check ID assignment
        console.log(" Setting homeTeamId:", homeTeamId, "for", result.data.homeTeam);
        console.log(" Setting awayTeamId:", awayTeamId, "for", result.data.awayTeam);
    
        if (index !== undefined) {
          this.matchesArray.at(index).setValue({
            ...result.data,
            homeTeamId: homeTeamId,
            awayTeamId: awayTeamId
          });
        } else {
          const newMatchGroup = this.fb.group({
            matchId: [result.data.matchId], // Preserve match ID
            stage: [result.data.stage], // Optional
            homeTeamId: [homeTeamId], // Dynamically set ID
            homeTeam: [result.data.homeTeam, Validators.required],
            awayTeamId: [awayTeamId], // Dynamically set ID
            awayTeam: [result.data.awayTeam, Validators.required],
            matchStart: [result.data.matchStart, Validators.required],
            betType: [result.data.betType, Validators.required],
            homeWinOdds: [result.data.homeWinOdds, Validators.required],
            drawOdds: [result.data.drawOdds, Validators.required],
            awayWinOdds: [result.data.awayWinOdds, Validators.required],
            homeQualifies: [result.data.homeQualifies, Validators.required],
            awayQualifies: [result.data.awayQualifies, Validators.required],
          });
    
          this.matchesArray.push(newMatchGroup);
          this.matchesArray.updateValueAndValidity();
        }
      }
    });
        
    return await modal.present();
  }
  
  generateMatchId(): number {
    return Math.floor(100000 + Math.random() * 900000); // Generate a unique match ID
  }
  
  findTeamIdByName(teamName: string): number | null {
    if (!teamName) {
      console.warn(" findTeamIdByName was called with an empty teamName");
      return null; // Handle undefined or empty names
    }
  
    // Debugging log: Print current teams
    console.log(" Looking up ID for:", teamName);
    console.log(" Current Teams Array:", this.teamsArray.value);
  
    const teamIndex = this.teamsArray.value.findIndex((team: string) => 
      team.trim().toLowerCase() === teamName.trim().toLowerCase()
    );
  
    if (teamIndex === -1) {
      console.warn(` Team ID not found for: ${teamName}`);
      return null; // If team is not found, return null
    }
  
    console.log(` Found Team ID: ${teamIndex + 1} for ${teamName}`);
    return teamIndex + 1; // Return the ID (index + 1)
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
            matchStart: [matchDateTime, Validators.required],
            betType: [row['Bet Type'], Validators.required],
            homeWinOdds: [this.parseOdds(row['Home Win Odds']), Validators.required],
            drawOdds: [this.parseOdds(row['Draw Odds']), Validators.required],
            awayWinOdds: [this.parseOdds(row['Away Win Odds']), Validators.required],
            homeQualifies: [this.parseOdds(row['Home Team Qualifies'])],
            awayQualifies: [this.parseOdds(row['Away Team Qualifies'])]
          }));
        }
      });
    }
  }
   
  convertToTimestamp(date: string, time: string, utcOffset: number): string {
    // Combine date and time into a single string
    const dateTimeString = `${date}T${time}`;
    
    // Convert to Date object
    let localDate = new Date(dateTimeString);
    
    // Apply UTC offset correctly
    localDate.setHours(localDate.getHours() - utcOffset);
    
    // Convert to ISO string to ensure proper format
    const utcDateTime = localDate.toISOString();
    
    console.log(`Converted Timestamp: ${utcDateTime} (Original: ${dateTimeString}, Offset: ${utcOffset})`); // Debug log
  
    return utcDateTime;
  }
  
  parseOdds(odds: any): number | null {
    if (typeof odds === 'string') {
      const parsed = parseFloat(odds.replace(',', '.'));
      return isNaN(parsed) ? null : parsed;
    }
    return typeof odds === 'number' && !isNaN(odds) ? odds : null;
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
  
  async removeTeam(index: number) {
    const teamName = this.teamsArray.at(index).value;

    const alert = await this.alertController.create({
      header: 'Confirm Deletion',
      message: `Deleting ${teamName} will also remove all related matches. Do you want to continue?`,
      buttons: [
        {
          text: 'Cancel',
          role: 'cancel'
        },
        {
          text: 'Delete',
          handler: () => {
            this.teamsArray.removeAt(index);
            this.removeMatchesByTeam(teamName);
            this.showToast(`Removed team: ${teamName}`, 'success');
          }
        }
      ]
    });

    await alert.present();
  }

  removeMatchesByTeam(teamName: string) {
    this.matchesArray.controls = this.matchesArray.controls.filter(
      match => match.value.homeTeam !== teamName && match.value.awayTeam !== teamName
    );
    this.matchesArray.updateValueAndValidity();
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
    // Validation Checks
    if (!this.tournamentForm.value.tournamentName?.trim()) {
      this.showToast('Tournament name is required!', 'danger');
      return;
    }

    if (this.teamsArray.length < 2) {
      this.showToast('At least 2 teams are required to create a tournament!', 'danger');
      return;
    }

    if (this.matchesArray.length < 1) {
      this.showToast('At least 1 match is required!', 'danger');
      return;
    }

    // Prepare Data for Submission
    const tournamentData: Tournament = {
      tournamentId: this.tournamentId ?? null, // Use null if new tournament
      tournamentName: this.tournamentForm.value.tournamentName,
      isActive: true,
      createdBy: this.tournamentForm.value.createdBy || 'Admin', // Default if missing
      createdAt: this.tournamentForm.value.createdAt || new Date().toISOString(),
      teams: this.teamsArray.value.map((teamName: string, index: number) => ({
        id: index + 1,
        teamName,
      })),
      matches: this.matchesArray.value.map((match: any) => ({
        ...match,
        matchStart: new Date(match.matchStart).toISOString(), // Ensure ISO format
      })),
    };

    console.log('Submitting Tournament:', tournamentData);

    // API Call with Redirection on Success
    const submitObservable = this.tournamentId
      ? this.tournamentService.updatePredefinedTournament(tournamentData)
      : this.tournamentService.createPredefinedTournament(tournamentData);

    submitObservable.subscribe({
      next: () => {
        // Navigate first
        this.router.navigate(['/predefined-tournaments']).then(() => {
          // After navigation, show the toast
          setTimeout(() => {
            this.showToast('Tournament saved successfully!', 'success');
          }, 500); // Small delay to ensure UI update
        });
      },
      error: (error) => {
        console.error('Error submitting tournament:', error);
        this.showToast('Error submitting tournament!', 'danger');
      },
    });
  }

  resetForm() {
    this.tournamentForm.reset(); // Clear form data
    this.teamsArray.clear(); // Clear teams
    this.matchesArray.clear(); // Clear matches
    this.teamMap = {}; // Reset team map
    this.file = null; // Reset file
    this.step = 1; // Reset to Step 1
  }
}
