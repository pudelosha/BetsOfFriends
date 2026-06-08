import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { ModalController, ToastController } from '@ionic/angular';
import { IonHeader, IonToolbar, IonTitle, IonButtons, IonButton, IonContent, IonLabel, IonInput } from '@ionic/angular/standalone';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { Team } from 'src/app/model/tournament-model';

@Component({
  selector: 'app-edit-team-modal',
  templateUrl: './edit-team-modal.component.html',
  styleUrls: ['./edit-team-modal.component.scss'],
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TranslateModule, IonHeader, IonToolbar, IonTitle, IonButtons, IonButton, IonContent, IonLabel, IonInput],
})
export class EditTeamModalComponent implements OnInit {
  @Input() team: Team | null = null;
  @Input() isEditing: boolean = false;
  @Input() allTeamNames: string[] = [];

  teamForm: FormGroup;

  constructor(
    private modalController: ModalController,
    private fb: FormBuilder,
    private toastController: ToastController,
    private translate: TranslateService
  ) {
    this.teamForm = this.fb.group({
      teamFrontendId: [''],
      teamId: [null],
      externalTeamId: [null],
      predefinedTeamId: [null],
      teamName: ['', [Validators.required, Validators.maxLength(50)]],
      eloRating: [1000, [Validators.required, Validators.min(0), Validators.max(5000)]],
      recordStatus: ['New'],
    });
  }

  ngOnInit(): void {
    if (this.team) {
      this.teamForm.patchValue({
        teamFrontendId: this.team.teamFrontendId || this.generateFrontendId(),
        teamId: this.team.teamId ?? null,
        externalTeamId: this.team.externalTeamId ?? null,
        predefinedTeamId: this.team.predefinedTeamId ?? null,
        teamName: this.team.teamName,
        eloRating: this.team.eloRating ?? 1000, // <--- default
        recordStatus: this.team.recordStatus ?? 'Uploaded',
      });
    } else {
      this.teamForm.patchValue({ 
        teamFrontendId: this.generateFrontendId(),
        eloRating: 1000, // <--- default
        recordStatus: 'New'
      });
    }
  }

  private generateFrontendId(): string {
    return 'T-' + Math.random().toString(36).substr(2, 9);
  }

  async saveTeam(): Promise<void> {
    if (this.teamForm.invalid) {
      await this.showToast(this.t('TOASTS.EDIT_TEAM_INVALID_DATA'), 'danger');
      return;
    }

    const teamName = this.teamForm.value.teamName.trim().toLowerCase();
    const currentTeamName = this.team?.teamName?.trim().toLowerCase() || '';

    const existingTeamNames = this.isEditing
      ? this.allTeamNames.filter(name => name !== currentTeamName)
      : this.allTeamNames;

    if (existingTeamNames.includes(teamName)) {
      await this.showToast(this.t('TOASTS.TEAM_NAME_EXISTS'), 'danger');
      return;
    }

    const isUpdated = this.isEditing && teamName !== currentTeamName;

    const elo = Number(this.teamForm.value.eloRating);
    if (Number.isNaN(elo)) {
      await this.showToast(this.t('TOASTS.TEAM_ELO_INVALID'), 'danger');
      return;
    }

    const updatedTeam: Team = {
      teamFrontendId: this.teamForm.value.teamFrontendId,
      teamId: this.teamForm.value.teamId,
      externalTeamId: this.teamForm.value.externalTeamId ?? null,
      predefinedTeamId: this.teamForm.value.predefinedTeamId ?? null,
      teamName: this.teamForm.value.teamName.trim(),
      eloRating: elo,
      recordStatus: this.isEditing
        ? (isUpdated ? 'Update' : this.teamForm.value.recordStatus)
        : 'New',
    };

    await this.modalController.dismiss(updatedTeam);
    await this.showToast(
      this.t(this.isEditing ? 'TOASTS.TEAM_UPDATED' : 'TOASTS.TEAM_ADDED'),
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

  private t(key: string, params?: Record<string, unknown>): string {
    return this.translate.instant(key, params);
  }
}
