import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { ModalController } from '@ionic/angular';
import { IonButton, IonContent, IonHeader, IonIcon, IonTitle, IonToolbar } from '@ionic/angular/standalone';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-delete-all-messages-modal',
  templateUrl: './delete-all-messages-modal.component.html',
  styleUrls: ['./delete-all-messages-modal.component.scss'],
  standalone: true,
  imports: [CommonModule, TranslateModule, IonHeader, IonToolbar, IonTitle, IonContent, IonButton, IonIcon],
})
export class DeleteAllMessagesModalComponent {
  constructor(private modalController: ModalController) {}

  dismiss(): void {
    this.modalController.dismiss({ confirmed: false });
  }

  confirm(): void {
    this.modalController.dismiss({ confirmed: true });
  }
}
