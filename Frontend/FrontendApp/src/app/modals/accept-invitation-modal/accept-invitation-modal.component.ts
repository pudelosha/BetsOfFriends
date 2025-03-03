import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonicModule, ToastController, LoadingController, AlertController, ModalController  } from '@ionic/angular';
import { ReactiveFormsModule } from '@angular/forms';

@Component({
  selector: 'app-accept-invitation-modal',
  templateUrl: './accept-invitation-modal.component.html',
  styleUrls: ['./accept-invitation-modal.component.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule],
})
export class AcceptInvitationModalComponent {
  @Input() tournamentName!: string;

  constructor(private modalController: ModalController) {}

  confirm() {
    this.modalController.dismiss({ accepted: true });
  }

  dismiss() {
    this.modalController.dismiss({ accepted: false });
  }
}
