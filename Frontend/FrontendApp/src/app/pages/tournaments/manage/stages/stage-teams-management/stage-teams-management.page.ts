import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { ToastController } from '@ionic/angular';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, FormArray, FormControl } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ModalController } from '@ionic/angular';
import { EditTeamModalComponent } from 'src/app/modals/edit-team-modal/edit-team-modal.component';
import { AlertController } from '@ionic/angular';
import { Team } from 'src/app/model/tournament-model';
import { TranslateModule } from '@ngx-translate/core';
import { IonList, IonItem, IonButton, IonIcon } from '@ionic/angular/standalone';

@Component({
  selector: 'app-stage-teams-management',
  templateUrl: './stage-teams-management.page.html',
  styleUrls: ['./stage-teams-management.page.scss'],
  standalone: true,
  imports: [IonIcon, CommonModule, ReactiveFormsModule, TranslateModule, IonList, IonItem, IonButton],
})
export class StageTeamsManagementPage implements OnInit {
  @Input() teamsArray!: FormArray;
  @Output() teamsUpdated = new EventEmitter<{ previousTeams: Team[]; updatedTeams: Team[] }>();

  isMobile = false;

  constructor(
    private toastController: ToastController,
    private modalController: ModalController,
    private alertController: AlertController,
    private fb: FormBuilder
  ) {}

  ngOnInit(): void {
    this.checkScreenSize();
    window.addEventListener('resize', this.checkScreenSize.bind(this));
  }

  checkScreenSize(): void {
    this.isMobile = window.innerWidth < 600;
  }

  getDeleteIcon(status: string): string {
    switch (status) {
      case 'Delete': return 'arrow-undo-outline';
      case 'Uploaded':
      case 'Update':
      case 'New':
      default:
        return 'trash-outline';
    }
  }
  
  getTeamControl(index: number): FormGroup {
    return this.teamsArray.at(index) as FormGroup;
  }

  private generateFrontendId(): string {
    return 'T-' + Math.random().toString(36).substr(2, 9);
  }

  async addTeam(): Promise<void> {
    const allTeamNames = this.teamsArray.controls.map(control =>
      control.get('teamName')?.value.trim().toLowerCase()
    );

    const modal = await this.modalController.create({
      component: EditTeamModalComponent,
      componentProps: {
        team: null,
        isEditing: false,
        allTeamNames,
      },
    });

    modal.onDidDismiss().then(result => {
      if (result.data) {
        const newTeam: Team = {
          teamFrontendId: this.generateFrontendId(),
          teamId: null,
          teamName: result.data.teamName.trim(),
          recordStatus: 'New'
        };

        this.teamsArray.push(this.fb.group(newTeam));
        this.emitTeams();
      }
    });

    await modal.present();
  }

  async editTeam(index: number): Promise<void> {
    const team = this.teamsArray.at(index).value;
    const allTeamNames = this.teamsArray.controls.map(control =>
      control.get('teamName')?.value.trim().toLowerCase()
    );

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

        if (updatedTeam.teamName.trim() !== teamGroup.get('teamName')?.value.trim()) {
          teamGroup.patchValue({
            teamName: updatedTeam.teamName.trim(),
            recordStatus: 'Update'
          });
        }

        this.emitTeams(previousTeamsArray);
      }
    });

    await modal.present();
  }

  async handleRemoveOrUndoTeam(index: number): Promise<void> {
    const teamControl = this.getTeamControl(index);
    const teamToRemove = teamControl.value;
    const currentStatus = teamToRemove.recordStatus;
  
    if (currentStatus === 'Delete') {
      teamControl.patchValue({ recordStatus: 'Update' });
      this.emitTeams();
      await this.showToast(`Team "${teamToRemove.teamName}" restored successfully!`, 'success');
    } else {
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
                this.teamsArray.removeAt(index);
              } else {
                teamControl.patchValue({ recordStatus: 'Delete' });
              }
  
              this.emitTeams();
              await this.showToast(`Team "${teamToRemove.teamName}" removed successfully!`, 'success');
            },
          },
        ],
      });
  
      await alert.present();
    }
  }
  
  getDeleteButtonText(recordStatus: string | null): string {
    return recordStatus === 'Delete' ? 'Undo' : 'Delete';
  }
  
  getDeleteButtonColor(recordStatus: string | null): string {
    return recordStatus === 'Delete' ? 'medium' : 'danger';
  }
  
  private emitTeams(previousTeams?: Team[]): void {
    const updatedTeams: Team[] = this.teamsArray.value.map((team: any) => ({
      teamFrontendId: team.teamFrontendId,
      teamId: team.teamId,
      teamName: team.teamName,
      recordStatus: team.recordStatus ?? 'Uploaded'
    }));

    this.teamsUpdated.emit({
      previousTeams: previousTeams ?? updatedTeams,
      updatedTeams,
    });
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