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
      frontendId: [''], // Always required
      backendId: [null], // Only exists for existing teams
      teamName: ['', [Validators.required, Validators.maxLength(50)]],
    });
  }

  ngOnInit(): void {
    if (this.team) {
      this.teamForm.patchValue(this.team); // Populate the form with team data if provided
    } else {
      // If it's a new team, generate a frontendId
      this.teamForm.patchValue({ frontendId: this.generateFrontendId() });
    }
  }

  // Generate unique frontendId for new teams
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

    // Prepare the structured team object
    const updatedTeam: Team = {
      frontendId: this.teamForm.value.frontendId,
      backendId: this.teamForm.value.backendId, // Preserve backendId if available
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
