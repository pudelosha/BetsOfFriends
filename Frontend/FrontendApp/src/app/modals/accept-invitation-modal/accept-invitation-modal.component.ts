import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { ModalController, ToastController } from '@ionic/angular';
import { IonHeader, IonToolbar, IonTitle, IonButtons, IonButton, IonContent, IonLabel, IonInput, IonSpinner, IonIcon } from '@ionic/angular/standalone';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { CustomTournamentService } from 'src/app/services/custom-tournament.service';
import { TournamentSelectionService } from 'src/app/services/tournament-selection.service';
import { firstValueFrom } from 'rxjs';
import { BackendMessageService } from 'src/app/services/backend-message.service';

@Component({
  selector: 'app-accept-invitation-modal',
  templateUrl: './accept-invitation-modal.component.html',
  styleUrls: ['./accept-invitation-modal.component.scss'],
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TranslateModule, IonHeader, IonToolbar, IonTitle, IonButtons, IonButton, IonContent, IonLabel, IonInput, IonSpinner, IonIcon],
})
export class AcceptInvitationModalComponent implements OnInit {
  @Input() tournamentName!: string;
  @Input() tournamentId!: number;
  @Input() editMode: boolean = false;

  nicknameForm: FormGroup;
  isLoading = false;
  nicknameError: string | null = null;

  constructor(
    private modalController: ModalController,
    private fb: FormBuilder,
    private tournamentService: CustomTournamentService,
    private toastController: ToastController,
    private tournamentSelectionService: TournamentSelectionService,
    private translate: TranslateService,
    private backendMessages: BackendMessageService
  ) {
    this.nicknameForm = this.fb.group({
      nickname: ['', [Validators.required, Validators.maxLength(20)]],
    });
  }

  ngOnInit() {
    if (this.editMode) {
      this.loadAssignment();
    }

    this.nicknameForm.get('nickname')!.valueChanges.subscribe(() => {
      this.nicknameError = null;
    });
  }

  async loadAssignment() {
    try {
      const assignment = await firstValueFrom(this.tournamentService.getAssignmentDetails(this.tournamentId));
      this.nicknameForm.patchValue({ nickname: assignment.nickname });
    } catch (error) {
      this.nicknameError = this.t('INVITE.LOAD_ASSIGNMENT_FAILED');
    }
  }

  async confirm() {
    if (this.nicknameForm.invalid) return;
    this.isLoading = true;
    this.nicknameError = null;
    const nickname = this.nicknameForm.value.nickname;

    try {
      if (this.editMode) {
        const response = await firstValueFrom(
          this.tournamentService.updateTournamentAssignment(this.tournamentId, nickname)
        );
        this.showToast(
          this.backendMessages.translateMessage(response.message, 'INVITE.NICKNAME_UPDATED', true),
          'success'
        );
      } else {
        const response = await firstValueFrom(
          this.tournamentService.acceptTournamentInvitation(this.tournamentId, nickname)
        );
        this.showToast(this.t('INVITE.ACCEPTED', { nickname }), 'success');
        this.tournamentSelectionService.setSelectedTournament(this.tournamentId);
      }

      this.modalController.dismiss({ accepted: true, nickname });

    } catch (error: any) {
      console.error('Error:', error);
      this.nicknameError = this.backendMessages.translateMessage(
        this.extractBackendMessage(error),
        this.editMode ? 'INVITE.UPDATE_FAILED' : 'INVITE.ACCEPT_FAILED'
      );
    } finally {
      this.isLoading = false;
    }
  }

  async quitTournament() {
    try {
      await firstValueFrom(this.tournamentService.quitTournament(this.tournamentId));
      this.showToast(this.t(this.editMode ? 'INVITE.LEFT' : 'INVITE.REJECTED'), 'success');
      this.modalController.dismiss({ accepted: false, quit: true });
    } catch (error: any) {
      this.nicknameError = this.backendMessages.translateMessage(this.extractBackendMessage(error), 'INVITE.LEAVE_FAILED');
    }
  }

  dismiss() {
    this.modalController.dismiss({ accepted: false });
  }

  async showToast(message: string, color: 'success' | 'danger') {
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

  private extractBackendMessage(error: unknown): string | undefined {
    const maybeHttpError = error as { error?: { message?: string; Message?: string } };
    return maybeHttpError?.error?.message || maybeHttpError?.error?.Message;
  }
}
