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
  @Input() match: any; // Match object (existing or new)
  @Input() index?: number; // Index of match in list (undefined if new)
  @Input() teams: string[] = []; // Available teams for selection

  matchForm: FormGroup;

  constructor(
    private modalController: ModalController,
    private fb: FormBuilder,
    private toastController: ToastController
  ) {
    // Define Reactive Form Structure
    this.matchForm = this.fb.group({
      matchId: [this.match?.matchId || null],
      stage: [this.match?.stage || ''],
      homeTeamId: [this.match?.homeTeamId || null],
      homeTeam: [this.match?.homeTeam || '', Validators.required],
      awayTeamId: [this.match?.awayTeamId || null],
      awayTeam: [this.match?.awayTeam || '', Validators.required],
      matchStart: [this.match?.matchStart || '', Validators.required],
      betType: [this.match?.betType || '', Validators.required],
      homeWinOdds: [this.match?.homeWinOdds || null, Validators.required],
      drawOdds: [this.match?.drawOdds || null, Validators.required],
      awayWinOdds: [this.match?.awayWinOdds || null, Validators.required],
      homeQualifies: [this.match?.homeQualifies || null],
      awayQualifies: [this.match?.awayQualifies || null],
    });      
  }

  ngOnInit() {
    if (this.match) {
      this.matchForm.patchValue(this.match);
    }
  }

  async saveMatch() {
    console.log('Form Status:', this.matchForm.status); // Should be VALID
    console.log('Form Errors:', this.matchForm.errors); // Should be null
    console.log('Form Values:', this.matchForm.value);  // Check required fields

    if (this.matchForm.invalid) {
      this.showToast('Please fill in all required fields!', 'danger');
      return;
    }
  
    const matchData = {
      matchId: this.match?.matchId || null, // Retain ID for existing matches
      homeTeamId: this.match?.homeTeamId || null, // Retain ID for existing matches
      awayTeamId: this.match?.awayTeamId || null, // Retain ID for existing matches
      stage: this.matchForm.value.stage || null, // Set to null if empty
      homeTeam: this.matchForm.value.homeTeam || '',
      awayTeam: this.matchForm.value.awayTeam || '',
      matchStart: this.matchForm.value.matchStart || '',
      betType: this.matchForm.value.betType || '90min', // Ensure betType is included
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
}
