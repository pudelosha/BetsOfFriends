import { Component, Input, OnInit } from '@angular/core';
import { IonicModule, ModalController, ToastController } from '@ionic/angular';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';

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
  @Input() teams: any[] = []; // Available teams for selection

  matchForm: FormGroup;

  constructor(
    private modalController: ModalController,
    private fb: FormBuilder,
    private toastController: ToastController
  ) {
    this.matchForm = this.fb.group({
      frontendId: [null],  
      backendId: [null],    

      stage: ['', Validators.required],  
      homeTeamFrontendId: [null],  
      homeTeamId: [null],  
      homeTeam: ['', Validators.required],  

      awayTeamFrontendId: [null],  
      awayTeamId: [null],  
      awayTeam: ['', Validators.required],  

      matchStart: ['', Validators.required],  
      betType: ['90min', Validators.required],  
      homeWinOdds: [null, Validators.required],  
      drawOdds: [null, Validators.required],  
      awayWinOdds: [null, Validators.required],  
      homeQualifies: [null],  
      awayQualifies: [null],  
    });
  }

  ngOnInit() {
    if (this.match) {
      // Ensure `frontendId` is retained
      this.matchForm.patchValue({
        ...this.match,
        frontendId: this.match.frontendId || this.generateFrontendId(),
        homeTeamFrontendId: this.match.homeTeamFrontendId || null,
        awayTeamFrontendId: this.match.awayTeamFrontendId || null,
      });
    } else {
      // Generate `frontendId` for new matches
      this.matchForm.patchValue({
        frontendId: this.generateFrontendId(),
      });
    }
  }

  async saveMatch() {
    if (this.matchForm.invalid) {
      this.showToast('Please fill in all required fields!', 'danger');
      return;
    }

    // Find selected team objects to ensure correct frontend and backend IDs
    const selectedHomeTeam = this.teams.find(t => t.teamName === this.matchForm.value.homeTeam);
    const selectedAwayTeam = this.teams.find(t => t.teamName === this.matchForm.value.awayTeam);

    if (!selectedHomeTeam || !selectedAwayTeam) {
      this.showToast('Invalid team selection!', 'danger');
      return;
    }

    const matchData = {
      frontendId: this.matchForm.value.frontendId, // Ensure frontendId is retained
      backendId: this.match?.backendId || null, // Retain backendId if editing

      stage: this.matchForm.value.stage || null,

      homeTeamFrontendId: selectedHomeTeam.frontendId, // Ensure correct frontend ID
      homeTeamId: selectedHomeTeam.backendId || null, // Use backend ID if available
      homeTeam: selectedHomeTeam.teamName,

      awayTeamFrontendId: selectedAwayTeam.frontendId, // Ensure correct frontend ID
      awayTeamId: selectedAwayTeam.backendId || null, // Use backend ID if available
      awayTeam: selectedAwayTeam.teamName,

      matchStart: this.matchForm.value.matchStart || '',
      betType: this.matchForm.value.betType || '90min',
      homeWinOdds: this.matchForm.value.homeWinOdds || null,
      drawOdds: this.matchForm.value.drawOdds || null,
      awayWinOdds: this.matchForm.value.awayWinOdds || null,
      homeQualifies: this.matchForm.value.homeQualifies || null,
      awayQualifies: this.matchForm.value.awayQualifies || null,
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
    return 'match-' + Math.random().toString(36).substr(2, 9);
  }
}
