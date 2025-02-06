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
      matchId: [null],
      stage: ['', Validators.required],
      homeTeamId: [null, Validators.required],
      homeTeam: ['', Validators.required],
      awayTeamId: [null, Validators.required],
      awayTeam: ['', Validators.required],
      date: ['', Validators.required],
      betType: ['', Validators.required],
      homeWinOdds: ['', [Validators.required, Validators.min(1)]],
      drawOdds: ['', [Validators.required, Validators.min(1)]],
      awayWinOdds: ['', [Validators.required, Validators.min(1)]],
      homeQualifies: ['', [Validators.required, Validators.min(1)]],
      awayQualifies: ['', [Validators.required, Validators.min(1)]],
    });       
  }

  ngOnInit() {
    if (this.match) {
      this.matchForm.patchValue(this.match); // Populate form if editing
    }
  }

  async saveMatch() {
    if (this.matchForm.invalid) {
      this.showToast('Please fill in all required fields with valid values!', 'danger');
      return;
    }

    const matchData = this.matchForm.value;
    this.modalController.dismiss(matchData);

    if (this.index !== undefined) {
      this.showToast('Match updated successfully!', 'success');
    } else {
      this.showToast('New match added successfully!', 'success');
    }
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
