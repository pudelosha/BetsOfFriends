import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonicModule, ToastController, ModalController } from '@ionic/angular';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { CustomTournamentService } from 'src/app/services/custom-tournament.service';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-accept-invitation-modal',
  templateUrl: './accept-invitation-modal.component.html',
  styleUrls: ['./accept-invitation-modal.component.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule],
})
export class AcceptInvitationModalComponent implements OnInit {
  @Input() tournamentName!: string;
  @Input() tournamentId!: number;

  nicknameForm: FormGroup;
  isLoading = false;
  nicknameError: string | null = null;

  constructor(
    private modalController: ModalController,
    private fb: FormBuilder,
    private tournamentService: CustomTournamentService,
    private toastController: ToastController
  ) {
    this.nicknameForm = this.fb.group({
      nickname: ['', [Validators.required, Validators.maxLength(20)]],
    });
  }

  ngOnInit() {
    // Listen for nickname input changes and clear backend error message
    this.nicknameForm.get('nickname')!.valueChanges.subscribe(() => {
      this.nicknameError = null;
    });
  }

  async confirm() {
    if (this.nicknameForm.invalid) return;
  
    this.isLoading = true;
    this.nicknameError = null;
    const nickname = this.nicknameForm.value.nickname;
  
    try {
      // Call backend API
      const response = await firstValueFrom(
        this.tournamentService.acceptTournamentInvitation(this.tournamentId, nickname)
      );
  
      this.showToast(response.message, 'success');
      this.modalController.dismiss({ accepted: true });
  
    } catch (error: any) {
      console.error('Error accepting invitation:', error);
      this.nicknameError = error?.error?.message || 'An unexpected error occurred.';
    } finally {
      this.isLoading = false;
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
}
