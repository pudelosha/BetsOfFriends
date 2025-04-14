import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { ModalController, ToastController } from '@ionic/angular';
import { IonHeader, IonToolbar, IonTitle, IonButtons, IonButton, IonContent, IonLabel, IonInput } from '@ionic/angular/standalone';
import { TranslateModule } from '@ngx-translate/core';
import { Team } from 'src/app/model/tournament-model';

@Component({
  selector: 'app-edit-team-modal',
  templateUrl: './edit-team-modal.component.html',
  styleUrls: ['./edit-team-modal.component.scss'],
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TranslateModule, IonHeader, IonToolbar, IonTitle, IonButtons, IonButton, IonContent, IonLabel, IonInput],
})
export class EditTeamModalComponent implements OnInit {
  @Input() team: Team | null = null; // Input to receive team details
  @Input() isEditing: boolean = false; // Indicates if the modal is for editing or adding
  @Input() allTeamNames: string[] = []; // List of existing team names

  teamForm: FormGroup;

  constructor(
    private modalController: ModalController,
    private fb: FormBuilder,
    private toastController: ToastController
  ) {
    this.teamForm = this.fb.group({
      teamFrontendId: [''], // Always required (Renamed correctly)
      teamId: [null], // Backend ID (Renamed correctly)
      teamName: ['', [Validators.required, Validators.maxLength(50)]],
      recordStatus: ['New'], // Default to "New"
    });
  }

  ngOnInit(): void {
    if (this.team) {
      this.teamForm.patchValue({
        teamFrontendId: this.team.teamFrontendId || this.generateFrontendId(), // Ensure frontend ID
        teamId: this.team.teamId ?? null, // Preserve backend ID
        teamName: this.team.teamName,
        recordStatus: this.team.recordStatus ?? 'Uploaded', // Preserve status or default to "Uploaded"
      });
    } else {
      this.teamForm.patchValue({ 
        teamFrontendId: this.generateFrontendId(),
        recordStatus: 'New' // Default for new records
      });
    }
  }

  // Generate a unique teamFrontendId for new teams
  private generateFrontendId(): string {
    return 'T-' + Math.random().toString(36).substr(2, 9);
  }

  async saveTeam(): Promise<void> {
    if (this.teamForm.invalid) {
      await this.showToast('Please provide a valid team name!', 'danger');
      return;
    }

    const teamName = this.teamForm.value.teamName.trim().toLowerCase();
    const currentTeamName = this.team?.teamName?.trim().toLowerCase() || '';

    const existingTeamNames = this.isEditing
      ? this.allTeamNames.filter(name => name !== currentTeamName)
      : this.allTeamNames;

    if (existingTeamNames.includes(teamName)) {
      await this.showToast('Team name already exists. Please choose a different name.', 'danger');
      return;
    }

    // Check if a real update was made
    const isUpdated = this.isEditing && teamName !== currentTeamName;

    // Prepare the structured team object with correct naming
    const updatedTeam: Team = {
      teamFrontendId: this.teamForm.value.teamFrontendId,
      teamId: this.teamForm.value.teamId,
      teamName: this.teamForm.value.teamName.trim(),
      recordStatus: this.isEditing
        ? (isUpdated ? 'Update' : this.teamForm.value.recordStatus) // Only update if changed
        : 'New', // Default for new teams
    };

    await this.modalController.dismiss(updatedTeam);
    await this.showToast(
      this.isEditing ? 'Team updated successfully!' : 'New team added successfully!',
      'success'
    );
  }

  async closeModal(): Promise<void> {
    await this.modalController.dismiss(null);
  }

  private async showToast(message: string, color: 'success' | 'danger') {
    const toast = await this.toastController.create({
      message,
      duration: 3000,
      position: 'bottom',
      color,
    });
    await toast.present();
  }
}
