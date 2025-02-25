import { Component, Input, Output, EventEmitter } from '@angular/core';
import { IonicModule, ToastController } from '@ionic/angular';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, FormArray, FormControl } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ModalController } from '@ionic/angular';
import { EditTeamModalComponent } from 'src/app/modals/edit-team-modal/edit-team-modal.component';
import { AlertController } from '@ionic/angular';
import { Team } from 'src/app/model/tournament-model';

@Component({
  selector: 'app-stage-teams-management',
  templateUrl: './stage-teams-management.page.html',
  styleUrls: ['./stage-teams-management.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule],
})
export class StageTeamsManagementPage {
  @Input() teamsArray!: FormArray; // Input from parent for teams FormArray
  @Output() teamsUpdated = new EventEmitter<{ previousTeams: Team[]; updatedTeams: Team[] }>(); // Emits old and updated teams to parent

  constructor(
    private toastController: ToastController,
    private modalController: ModalController,
    private alertController: AlertController,
    private fb: FormBuilder
  ) {}

  // Get control for a specific team
  getTeamControl(index: number): FormGroup {
    return this.teamsArray.at(index) as FormGroup;
  }

  // Generate a unique frontendId for new teams
  private generateFrontendId(): string {
    return 'T-' + Math.random().toString(36).substr(2, 9);
  }

  // Add a new team
  async addTeam(): Promise<void> {
    const allTeamNames = this.teamsArray.controls.map(control =>
      control.get('teamName')?.value.trim().toLowerCase()
    );

    const modal = await this.modalController.create({
      component: EditTeamModalComponent,
      componentProps: {
        team: null, // New team
        isEditing: false,
        allTeamNames,
      },
    });

    modal.onDidDismiss().then(result => {
      if (result.data) {
        const newTeam: Team = {
          frontendId: this.generateFrontendId(), // Assign new frontend ID
          backendId: null, // New teams have no backend ID
          teamName: result.data.teamName.trim(),
        };

        this.teamsArray.push(this.fb.group(newTeam));

        console.log('Added New Team:', newTeam);
        this.emitTeams();
      }
    });

    await modal.present();
  }

  // Edit an existing team
  async editTeam(index: number): Promise<void> {
    const team = this.teamsArray.at(index).value;
    const allTeamNames = this.teamsArray.controls.map(control =>
      control.get('teamName')?.value.trim().toLowerCase()
    );

    // Snapshot previous state before editing
    const previousTeamsArray: Team[] = this.teamsArray.value.map((team: any) => ({ ...team }));

    const modal = await this.modalController.create({
      component: EditTeamModalComponent,
      componentProps: {
        team,
        isEditing: true,
        allTeamNames,
      },
    });

    modal.onDidDismiss().then(result => {
      if (result.data) {
        const updatedTeam = result.data;
        const teamGroup = this.teamsArray.at(index) as FormGroup;
        teamGroup.patchValue({
          teamName: updatedTeam.teamName.trim(), // Ensure trim to remove whitespace issues
        });

        console.log('Updated Team:', updatedTeam);

        // Emit updated teams with old reference
        this.emitTeams(previousTeamsArray);
      }
    });

    await modal.present();
  }

  // Remove a team
  async removeTeam(index: number): Promise<void> {
    const teamToRemove = this.teamsArray.at(index).value;

    // Snapshot previous state before deletion
    const previousTeamsArray: Team[] = this.teamsArray.value.map((team: any) => ({ ...team }));

    const alert = await this.alertController.create({
      header: 'Confirm Removal',
      message: `Are you sure you want to delete the team "${teamToRemove.teamName}"? Note: Any matches involving this team will also be affected.`,
      buttons: [
        {
          text: 'Cancel',
          role: 'cancel',
        },
        {
          text: 'Delete',
          role: 'destructive',
          handler: async () => {
            this.teamsArray.removeAt(index);

            console.log('Removed team:', teamToRemove);
            this.emitTeams(previousTeamsArray);
            await this.showToast(`Team "${teamToRemove.teamName}" removed successfully!`, 'success');
          },
        },
      ],
    });

    await alert.present();
  }

  // Emit updated teams to parent with previous state reference
  private emitTeams(previousTeams?: Team[]): void {
    const updatedTeams: Team[] = this.teamsArray.value.map((team: any) => ({
      frontendId: team.frontendId,
      backendId: team.backendId,
      teamName: team.teamName,
    }));

    this.teamsUpdated.emit({
      previousTeams: previousTeams ?? updatedTeams,
      updatedTeams,
    });

    console.log('Emitted Previous Teams:', previousTeams);
    console.log('Emitted Updated Teams:', updatedTeams);
  }

  // Show toast messages
  private async showToast(message: string, color: 'success' | 'warning' | 'danger' | 'primary'): Promise<void> {
    const toast = await this.toastController.create({
      message,
      duration: 3000,
      position: 'bottom',
      color,
    });
    await toast.present();
  }
}
