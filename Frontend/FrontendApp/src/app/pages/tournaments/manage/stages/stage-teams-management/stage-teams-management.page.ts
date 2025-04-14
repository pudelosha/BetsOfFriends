import { Component, Input, Output, EventEmitter } from '@angular/core';
import { ToastController } from '@ionic/angular';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, FormArray, FormControl } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ModalController } from '@ionic/angular';
import { EditTeamModalComponent } from 'src/app/modals/edit-team-modal/edit-team-modal.component';
import { AlertController } from '@ionic/angular';
import { Team } from 'src/app/model/tournament-model';
import { TranslateModule } from '@ngx-translate/core';
import { IonList, IonItem, IonButton } from '@ionic/angular/standalone';

@Component({
  selector: 'app-stage-teams-management',
  templateUrl: './stage-teams-management.page.html',
  styleUrls: ['./stage-teams-management.page.scss'],
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TranslateModule, IonList, IonItem, IonButton],
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
          teamFrontendId: this.generateFrontendId(), // Assign new frontend ID
          teamId: null, // New teams have no backend ID
          teamName: result.data.teamName.trim(),
          recordStatus: 'New' // Defaulting to 'New'
        };

        this.teamsArray.push(this.fb.group(newTeam));

        //console.log('Added New Team:', newTeam);
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

        // Check if the name actually changed before setting the status
        if (updatedTeam.teamName.trim() !== teamGroup.get('teamName')?.value.trim()) {
          teamGroup.patchValue({
            teamName: updatedTeam.teamName.trim(),
            recordStatus: 'Update'
          });
        }

        //console.log('Updated Team:', updatedTeam);
        this.emitTeams(previousTeamsArray);
      }
    });

    await modal.present();
  }

  // Remove a team
  async handleRemoveOrUndoTeam(index: number): Promise<void> {
    const teamControl = this.getTeamControl(index);
    const teamToRemove = teamControl.value;
    const currentStatus = teamToRemove.recordStatus;
  
    if (currentStatus === 'Delete') {
      // If already marked "Delete", undo by setting it to "Updated"
      teamControl.patchValue({ recordStatus: 'Update' });
      //console.log(`Undo delete action for team: ${teamToRemove.teamName}`);
      this.emitTeams();
      await this.showToast(`Team "${teamToRemove.teamName}" restored successfully!`, 'success');
    } else {
      // Otherwise, show confirmation alert before deleting or marking as "Delete"
      const alert = await this.alertController.create({
        header: 'Confirm Removal',
        message: `Are you sure you want to delete the team "${teamToRemove.teamName}"?`,
        buttons: [
          {
            text: 'Cancel',
            role: 'cancel',
          },
          {
            text: 'Delete',
            role: 'destructive',
            handler: async () => {
              if (currentStatus === 'New') {
                // If team is "New", remove it completely
                this.teamsArray.removeAt(index);
              } else {
                // Otherwise, mark it as "Delete"
                teamControl.patchValue({ recordStatus: 'Delete' });
              }
  
              //console.log('Updated team after removal action:', teamToRemove);
              this.emitTeams();
              await this.showToast(`Team "${teamToRemove.teamName}" removed successfully!`, 'success');
            },
          },
        ],
      });
  
      await alert.present();
    }
  }
  
  // Determines Delete vs Undo button text
  getDeleteButtonText(recordStatus: string | null): string {
    return recordStatus === 'Delete' ? 'Undo' : 'Delete';
  }
  
  // Determines button color based on record status
  getDeleteButtonColor(recordStatus: string | null): string {
    return recordStatus === 'Delete' ? 'medium' : 'danger';
  }
  
  // Emit updated teams to parent with previous state reference
  private emitTeams(previousTeams?: Team[]): void {
    const updatedTeams: Team[] = this.teamsArray.value.map((team: any) => ({
      teamFrontendId: team.teamFrontendId,
      teamId: team.teamId,
      teamName: team.teamName,
      recordStatus: team.recordStatus ?? 'Uploaded'  // Ensure default value
    }));

    this.teamsUpdated.emit({
      previousTeams: previousTeams ?? updatedTeams,
      updatedTeams,
    });

    //console.log('Emitted Previous Teams:', previousTeams);
    //console.log('Emitted Updated Teams:', updatedTeams);
  }

  getRecordStatusClass(recordStatus: string | null): string {
    switch (recordStatus) {
      case 'New': return 'team-status-new';
      case 'Update': return 'team-status-updated';
      case 'Delete': return 'team-status-delete';
      case 'Uploaded': return 'team-status-uploaded';
      default: return '';
    }
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