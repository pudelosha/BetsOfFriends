import { Component, Input, OnInit } from '@angular/core';
import { IonicModule, ModalController, ToastController } from '@ionic/angular';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { Team } from 'src/app/model/tournament-model';

@Component({
  selector: 'app-edit-team-modal',
  templateUrl: './edit-team-modal.component.html',
  styleUrls: ['./edit-team-modal.component.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule],
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
    });
  }

  ngOnInit(): void {
    if (this.team) {
      // Populate fields properly
      this.teamForm.patchValue({
        teamFrontendId: this.team.teamFrontendId || this.generateFrontendId(), // Ensure frontend ID
        teamId: this.team.teamId ?? null, // Preserve backend ID
        teamName: this.team.teamName,
      });
    } else {
      // Generate a new frontend ID for new teams
      this.teamForm.patchValue({ teamFrontendId: this.generateFrontendId() });
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

    // Prepare the structured team object with correct naming
    const updatedTeam: Team = {
      teamFrontendId: this.teamForm.value.teamFrontendId, // Matches new model name
      teamId: this.teamForm.value.teamId, // Preserve backendId if available
      teamName: this.teamForm.value.teamName.trim(),
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
