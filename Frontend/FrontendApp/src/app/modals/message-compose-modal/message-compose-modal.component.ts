import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ModalController } from '@ionic/angular';
import { IonButton, IonContent, IonHeader, IonLabel, IonSelect, IonSelectOption, IonTextarea, IonTitle, IonToolbar } from '@ionic/angular/standalone';
import { TranslateModule } from '@ngx-translate/core';
import { NotificationMessageRecipient } from 'src/app/services/notification.service';

@Component({
  selector: 'app-message-compose-modal',
  templateUrl: './message-compose-modal.component.html',
  styleUrls: ['./message-compose-modal.component.scss'],
  standalone: true,
  imports: [CommonModule, FormsModule, TranslateModule, IonHeader, IonToolbar, IonTitle, IonContent, IonLabel, IonSelect, IonSelectOption, IonTextarea, IonButton],
})
export class MessageComposeModalComponent {
  @Input() recipients: NotificationMessageRecipient[] = [];

  selectedRecipientAssignmentId: number | null = null;
  messageBody = '';

  constructor(private modalController: ModalController) {}

  dismiss(): void {
    this.modalController.dismiss(null);
  }

  send(): void {
    const message = this.messageBody.trim();
    if (!this.selectedRecipientAssignmentId || !message) {
      return;
    }

    this.modalController.dismiss({
      recipientAssignmentId: this.selectedRecipientAssignmentId,
      message
    });
  }
}
