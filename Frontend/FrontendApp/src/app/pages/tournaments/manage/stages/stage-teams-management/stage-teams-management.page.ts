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
  @Output() teamsUpdated = new EventEmitter<string[]>(); // Emits updated teams to parent

  constructor(private toastController: ToastController, private modalController: ModalController, private alertController: AlertController) {}

  // Get control for a specific team
  getTeamControl(index: number): FormGroup {
    return this.teamsArray.at(index) as FormGroup;
  }
  
  // Edit team
  async editTeam(index: number): Promise<void> {
    const team = this.teamsArray.at(index).value;
    const modal = await this.modalController.create({
      component: EditTeamModalComponent,
      componentProps: {
        team,
        isEditing: true,
      },
    });
  
    modal.onDidDismiss().then((result) => {
      if (result.data) {
        const updatedTeam = result.data;
        const teamGroup = this.teamsArray.at(index) as FormGroup;
        teamGroup.patchValue(updatedTeam);
        console.log('Updated Team:', updatedTeam);
      }
    });
  
    await modal.present();
  }

  // Remove a team
  async removeTeam(index: number): Promise<void> {
    const teamToRemove = this.teamsArray.at(index).value;
  
    // Show confirmation dialog using AlertController
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
          handler: () => {
            this.teamsArray.removeAt(index);
            this.emitTeams();
            console.log('Removed team:', teamToRemove);
          },
        },
      ],
    });
  
    await alert.present();
  }
    
  // Emit updated teams to parent
  private emitTeams(): void {
    const updatedTeams = this.teamsArray.value;
    this.teamsUpdated.emit(updatedTeams); // Emit updated teams to parent
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
