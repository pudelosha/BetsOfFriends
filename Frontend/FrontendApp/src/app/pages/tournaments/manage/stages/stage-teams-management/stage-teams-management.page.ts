import { Component, Input, Output, EventEmitter } from '@angular/core';
import { IonicModule, ToastController } from '@ionic/angular';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, FormArray, FormControl } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ModalController } from '@ionic/angular';
import { EditTeamModalComponent } from 'src/app/modals/edit-team-modal/edit-team-modal.component';
import { AlertController } from '@ionic/angular';

@Component({
  selector: 'app-stage-teams-management',
  templateUrl: './stage-teams-management.page.html',
  styleUrls: ['./stage-teams-management.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule],
})
export class StageTeamsManagementPage {
  @Input() teamsArray!: FormArray; // Input from parent for teams FormArray
  @Output() teamsUpdated = new EventEmitter<{ previousTeams: any[]; updatedTeams: any[] }>(); // Emits old and updated teams to parent

  constructor(private toastController: ToastController, private modalController: ModalController, private alertController: AlertController) {}

  // Get control for a specific team
  getTeamControl(index: number): FormGroup {
    return this.teamsArray.at(index) as FormGroup;
  }
  
  // Edit team
  async editTeam(index: number): Promise<void> {
    const team = this.teamsArray.at(index).value;
    const allTeamNames = this.teamsArray.controls.map((control) => control.get('teamName')?.value.trim().toLowerCase());

    // Create a snapshot of the current teamsArray to capture old state
    const previousTeamsArray = this.teamsArray.value.map((team: any) => ({ ...team }));

    const modal = await this.modalController.create({
      component: EditTeamModalComponent,
      componentProps: {
        team,
        isEditing: true,
        allTeamNames,
      },
    });
  
    modal.onDidDismiss().then((result) => {
      if (result.data) {
        const updatedTeam = result.data;
        const teamGroup = this.teamsArray.at(index) as FormGroup;
        teamGroup.patchValue(updatedTeam);
        console.log('Updated Team:', updatedTeam);

        // Emit both the previous and updated teams to the parent
        this.emitTeams(previousTeamsArray, this.teamsArray.value);
      }
    });
  
    await modal.present();
  }

  // Remove a team
  async removeTeam(index: number): Promise<void> {
    const teamToRemove = this.teamsArray.at(index).value;

    // Create a snapshot of the current teamsArray to capture old state
    const previousTeamsArray = this.teamsArray.value.map((team: any) => ({ ...team }));

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

            // Emit both the previous and updated teams to the parent
            this.emitTeams(previousTeamsArray, this.teamsArray.value);
            console.log('Removed team:', teamToRemove);

            await this.showToast(`Team "${teamToRemove.teamName}" removed successfully!`, 'success');
          },
        },
      ],
    });

    await alert.present();
  }
    
  // Emit updated teams to parent
  private emitTeams(previousTeams: any[], updatedTeams: any[]): void {
    this.teamsUpdated.emit({ previousTeams, updatedTeams }); // Emit old and updated teams to parent
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
