import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ModalController } from '@ionic/angular';
import { IonButton, IonCheckbox, IonContent, IonHeader, IonLabel, IonTextarea, IonTitle, IonToolbar } from '@ionic/angular/standalone';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-message-text-modal',
  templateUrl: './message-text-modal.component.html',
  styleUrls: ['./message-text-modal.component.scss'],
  standalone: true,
  imports: [CommonModule, FormsModule, TranslateModule, IonHeader, IonToolbar, IonTitle, IonContent, IonLabel, IonTextarea, IonButton, IonCheckbox],
})
export class MessageTextModalComponent {
  @Input() titleKey = 'MESSAGES.REPLY_TITLE';
  @Input() titleParams: Record<string, unknown> = {};
  @Input() descriptionKey?: string;
  @Input() descriptionParams: Record<string, unknown> = {};
  @Input() submitKey = 'MESSAGES.SEND';
  @Input() includeEmailOption = false;
  @Input() emailOptionKey = 'USER_MANAGER.SEND_EMAIL_COPY';

  messageBody = '';
  sendEmail = false;

  constructor(private modalController: ModalController) {}

  dismiss(): void {
    this.modalController.dismiss(null);
  }

  submit(): void {
    const message = this.messageBody.trim();
    if (!message) {
      return;
    }

    this.modalController.dismiss({ message, sendEmail: this.includeEmailOption ? this.sendEmail : false });
  }
}
