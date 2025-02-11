import { Component, Input, OnInit } from '@angular/core';
import { IonicModule, ModalController, ToastController } from '@ionic/angular';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-edit-team-modal',
  templateUrl: './edit-team-modal.component.html',
  styleUrls: ['./edit-team-modal.component.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule],
})
export class EditTeamModalComponent implements OnInit {
  @Input() team: { teamId: number | null; teamName: string } | null = null; // Input to receive team details
  @Input() isEditing: boolean = false; // Indicates if the modal is for editing or adding

  teamForm: FormGroup;

  constructor(
    private modalController: ModalController,
    private fb: FormBuilder,
    private toastController: ToastController
  ) {
    this.teamForm = this.fb.group({
      teamId: [null], // Nullable ID (only used for editing)
      teamName: ['', [Validators.required, Validators.maxLength(50)]],
    });
  }

  ngOnInit(): void {
    if (this.team) {
      this.teamForm.patchValue(this.team); // Populate the form with team data if provided
    }
  }

  async saveTeam(): Promise<void> {
    if (this.teamForm.invalid) {
      await this.showToast('Please provide a valid team name!', 'danger');
      return;
    }

    const teamData = this.teamForm.value;
    await this.modalController.dismiss(teamData);
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
