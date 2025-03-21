import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonicModule, ToastController, ModalController } from '@ionic/angular';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { CustomTournamentService } from 'src/app/services/custom-tournament.service';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-join-request-modal',
  templateUrl: './join-request-modal.component.html',
  styleUrls: ['./join-request-modal.component.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule],
})
export class JoinRequestModalComponent implements OnInit {
  @Input() tournamentId!: number;
  @Input() tournamentName!: string;

  requestForm: FormGroup;
  isLoading = false;
  nicknameError: string | null = null;

  constructor(
    private modalController: ModalController,
    private fb: FormBuilder,
    private tournamentService: CustomTournamentService,
    private toastController: ToastController
  ) {
    this.requestForm = this.fb.group({
      nickname: ['', [Validators.required, Validators.maxLength(20)]],
      message: ['']
    });
  }

  ngOnInit() {
    this.requestForm.get('nickname')?.valueChanges.subscribe(() => {
      this.nicknameError = null;
    });
  }

  async submitRequest() {
    if (this.requestForm.invalid) return;

    this.isLoading = true;
    const nickname = this.requestForm.value.nickname;
    const message = this.requestForm.value.message;

    try {
      await firstValueFrom(
        this.tournamentService.requestToJoinTournament(this.tournamentId, nickname, message)
      );

      await this.showToast('Request submitted successfully.', 'success');
      this.modalController.dismiss({ requested: true });
    } catch (error: any) {
      console.error('Error submitting join request:', error);
      this.nicknameError = error?.error?.message || 'An unexpected error occurred.';
    } finally {
      this.isLoading = false;
    }
  }

  dismiss() {
    this.modalController.dismiss({ requested: false });
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
