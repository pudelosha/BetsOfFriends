import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ModalController, ToastController } from '@ionic/angular';
import { IonHeader, IonToolbar, IonTitle, IonButtons, IonButton, IonContent, IonItem, IonLabel, IonTextarea } from '@ionic/angular/standalone';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-tournament-message-modal',
  templateUrl: './tournament-message-modal.component.html',
  styleUrls: ['./tournament-message-modal.component.scss'],
  standalone: true,
  imports: [CommonModule, FormsModule, TranslateModule, IonHeader, IonToolbar, IonTitle, IonButtons, IonButton, IonContent, IonItem, IonLabel, IonTextarea]
})
export class TournamentMessageModalComponent {
  messageContent: string = '';

  constructor(
    private modalCtrl: ModalController,
    private toastController: ToastController,
    private translate: TranslateService
  ) {}

  closeModal() {
    this.modalCtrl.dismiss();
  }

  async submitMessage() {
    if (!this.messageContent.trim()) {
      this.showToast(this.t('TOASTS.MESSAGE_REQUIRED'), 'warning');
      return;
    }

    if (this.messageContent.length > 100) {
      this.showToast(this.t('TOASTS.MESSAGE_TOO_LONG'), 'warning');
      return;
    }

    this.modalCtrl.dismiss({ content: this.messageContent.trim() });
  }

  private async showToast(message: string, color: 'success' | 'warning' | 'danger') {
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
