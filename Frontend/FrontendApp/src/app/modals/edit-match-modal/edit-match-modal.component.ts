import { Component, Input, OnInit } from '@angular/core';
import { IonicModule, ModalController, ToastController } from '@ionic/angular';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { Team, Stage } from 'src/app/model/tournament-model';

@Component({
  selector: 'app-edit-match-modal',
  templateUrl: './edit-match-modal.component.html',
  styleUrls: ['./edit-match-modal.component.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule],
})
export class EditMatchModalComponent implements OnInit {
  @Input() match: any; // Existing match (if editing), otherwise null
  @Input() index?: number; // Index in the match array
  @Input() teams: Team[] = [];
  @Input() stages: Stage[] = [];

  matchForm: FormGroup;

  constructor(
    private modalController: ModalController,
    private fb: FormBuilder,
    private toastController: ToastController
  ) {
    this.matchForm = this.fb.group({
      matchFrontendId: [null],  
      matchId: [null],    

      stageFrontendId: [null],
      stageId: [null],
      stageName: ['', Validators.required],

      homeTeamFrontendId: [null],  
      homeTeamId: [null],  
      homeTeam: ['', Validators.required],  

      awayTeamFrontendId: [null],  
      awayTeamId: [null],  
      awayTeam: ['', Validators.required],  

      matchStart: ['', Validators.required],  
      matchType: ['Regular90Min', Validators.required],  
      homeWinOdds: [null, Validators.required],  
      drawOdds: [null, Validators.required],  
      awayWinOdds: [null, Validators.required],  
      homeQualifies: [null],  
      awayQualifies: [null],  

      recordStatus: ['New'], // Default to "New"
    });

    // Listen for matchType changes
    this.matchForm.get('matchType')?.valueChanges.subscribe((value) => {
      this.toggleQualificationOddsValidation(value);
    });
  }

  ngOnInit() {
    if (this.match) {
      this.matchForm.patchValue({
        ...this.match,
        matchFrontendId: this.match.matchFrontendId || this.generateFrontendId(),
        homeTeamFrontendId: this.match.homeTeamFrontendId || null,
        awayTeamFrontendId: this.match.awayTeamFrontendId || null,
        stageFrontendId: this.match.stageFrontendId || null,
        recordStatus: this.match.recordStatus ?? 'Uploaded'
      });
    } else {
      this.matchForm.patchValue({
        matchFrontendId: this.generateFrontendId(),
        recordStatus: 'New'
      });
    }
  }  

  // Function to dynamically apply validation
  private toggleQualificationOddsValidation(matchType: string) {
    const homeQualifiesControl = this.matchForm.get('homeQualifies');
    const awayQualifiesControl = this.matchForm.get('awayQualifies');

    if (matchType === 'ExtendedWithQualification') {
      homeQualifiesControl?.setValidators([Validators.required]);
      awayQualifiesControl?.setValidators([Validators.required]);
    } else {
      homeQualifiesControl?.clearValidators();
      awayQualifiesControl?.clearValidators();
      homeQualifiesControl?.setValue(null);
      awayQualifiesControl?.setValue(null);
    }

    homeQualifiesControl?.updateValueAndValidity();
    awayQualifiesControl?.updateValueAndValidity();
  }

  async saveMatch() {
    if (this.matchForm.invalid) {
      this.showToast('Please fill in all required fields!', 'danger');
      return;
    }

    // If matchType is ExtendedWithQualification, validate homeQualifies and awayQualifies
    if (this.matchForm.value.matchType === 'ExtendedWithQualification') {
      if (this.matchForm.value.homeQualifies === null || this.matchForm.value.awayQualifies === null) {
        this.showToast('Please provide qualification odds for both teams!', 'danger');
        return;
      }
    }
  
    console.log('Teams Available:', this.teams);
    console.log('Searching for Home Team:', this.matchForm.value.homeTeam);
    console.log('Searching for Away Team:', this.matchForm.value.awayTeam);
    console.log('Searching for Stage:', this.matchForm.value.stageName);
  
    // Find selected team objects based on team name
    const selectedStage = this.stages.find(s => s.stageFrontendId === this.matchForm.value.stageFrontendId);
    const selectedHomeTeam = this.teams.find(t => t.teamFrontendId === this.matchForm.value.homeTeamFrontendId);
    const selectedAwayTeam = this.teams.find(t => t.teamFrontendId === this.matchForm.value.awayTeamFrontendId);
  
    console.log('Found Home Team:', selectedHomeTeam);
    console.log('Found Away Team:', selectedAwayTeam);
    console.log('Selected Stage:', selectedStage);
  
    if (!selectedHomeTeam || !selectedAwayTeam) {
      this.showToast('Invalid team selection!', 'danger');
      return;
    }

    if (!selectedStage) {
      this.showToast('Invalid stage selection!', 'danger');
      return;
    }

    const isUpdated = this.match &&
      (this.match.stageFrontendId !== selectedStage.stageFrontendId ||
      this.match.homeTeamFrontendId !== selectedHomeTeam.teamFrontendId ||
      this.match.awayTeamFrontendId !== selectedAwayTeam.teamFrontendId ||
      this.match.matchStart !== this.matchForm.value.matchStart ||
      this.match.matchType !== this.matchForm.value.matchType ||
      this.match.homeWinOdds !== this.matchForm.value.homeWinOdds ||
      this.match.drawOdds !== this.matchForm.value.drawOdds ||
      this.match.awayWinOdds !== this.matchForm.value.awayWinOdds ||
      this.match.homeQualifies !== this.matchForm.value.homeQualifies ||
      this.match.awayQualifies !== this.matchForm.value.awayQualifies);
  
    const matchData = {
      matchFrontendId: this.matchForm.value.matchFrontendId, // Ensure matchFrontendId is retained
      matchId: this.match?.matchId || null, // Retain matchId if editing
  
      stageFrontendId: selectedStage.stageFrontendId, // Use frontend ID for tracking
      stageId: selectedStage.stageId || null, // Backend ID (if available)
      stageName: selectedStage.stageName, // Ensure proper name assignment
  
      homeTeamFrontendId: selectedHomeTeam.teamFrontendId,
      homeTeamId: selectedHomeTeam.teamId || null,
      homeTeam: selectedHomeTeam.teamName,
    
      awayTeamFrontendId: selectedAwayTeam.teamFrontendId,
      awayTeamId: selectedAwayTeam.teamId || null,
      awayTeam: selectedAwayTeam.teamName,
  
      matchStart: this.matchForm.value.matchStart || '',
      matchType: this.matchForm.value.matchType || 'Regular90Min',
      homeWinOdds: this.matchForm.value.homeWinOdds || null,
      drawOdds: this.matchForm.value.drawOdds || null,
      awayWinOdds: this.matchForm.value.awayWinOdds || null,
      homeQualifies: this.matchForm.value.homeQualifies || null,
      awayQualifies: this.matchForm.value.awayQualifies || null,

      recordStatus: this.index !== undefined
        ? (isUpdated ? 'Updated' : this.matchForm.value.recordStatus) // Preserve if unchanged
        : 'New' // Default for new matches
    };
  
    console.log('Saving Match:', matchData);
  
    await this.modalController.dismiss(matchData);
    this.showToast(this.index !== undefined ? 'Match updated!' : 'New match added!', 'success');
  }  

  closeModal() {
    this.modalController.dismiss(null);
  }

  async showToast(message: string, color: 'success' | 'danger') {
    const toast = await this.toastController.create({
      message,
      duration: 3000,
      position: 'bottom',
      color,
    });
    await toast.present();
  }

  private generateFrontendId(): string {
    return 'M-' + Math.random().toString(36).substr(2, 9);
  }
}
