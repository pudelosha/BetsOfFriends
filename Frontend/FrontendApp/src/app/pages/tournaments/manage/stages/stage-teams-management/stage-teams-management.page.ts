import { Component, Input, Output, EventEmitter } from '@angular/core';
import { IonicModule, ToastController } from '@ionic/angular';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, FormArray, FormControl } from '@angular/forms';
import { CommonModule } from '@angular/common';

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

  constructor(private toastController: ToastController) {}

  // Get control for a specific team
  getTeamControl(index: number): FormControl {
    return this.teamsArray.at(index) as FormControl;
  }

  // Add a new team
  async addTeam(inputRef: any): Promise<void> {
    const teamName = inputRef.value?.trim();

    if (!teamName) {
      await this.showToast('Team name cannot be empty!', 'warning');
      return;
    }

    if (teamName.length > 50) {
      await this.showToast('Team name cannot exceed 50 characters!', 'warning');
      return;
    }

    // Check for duplicate team names
    const existingNames = this.teamsArray.value.map((name: string) => name.toLowerCase());
    if (existingNames.includes(teamName.toLowerCase())) {
      await this.showToast('Team already exists!', 'danger');
      return;
    }

    this.teamsArray.push(new FormControl(teamName, Validators.required)); // Add new team to FormArray
    inputRef.value = ''; // Clear the input field

    await this.showToast(`Added team: ${teamName}`, 'success'); // Show success message
    this.emitTeams(); // Notify parent about the updated teams
  }

  // Remove a team
  async removeTeam(index: number): Promise<void> {
    const teamName = this.teamsArray.at(index).value; // Get the team name
    this.teamsArray.removeAt(index); // Remove team from FormArray
    await this.showToast(`Removed team: ${teamName}`, 'success'); // Show success message
    this.emitTeams(); // Notify parent about the updated teams
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
