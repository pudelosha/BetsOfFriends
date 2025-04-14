import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { ModalController, ToastController } from '@ionic/angular';
import { IonHeader, IonToolbar, IonTitle, IonButtons, IonButton, IonContent, IonLabel, IonInput } from '@ionic/angular/standalone';
import { TranslateModule } from '@ngx-translate/core';
import { Stage } from 'src/app/model/tournament-model';

@Component({
  selector: 'app-edit-stage-modal',
  templateUrl: './edit-stage-modal.component.html',
  styleUrls: ['./edit-stage-modal.component.scss'],
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TranslateModule, IonHeader, IonToolbar, IonTitle, IonButtons, IonButton, IonContent, IonLabel, IonInput],
})
export class EditStageModalComponent implements OnInit {
  @Input() stage: Stage | null = null;
  @Input() isEditing: boolean = false;

  stageForm: FormGroup;

  constructor(
    private modalController: ModalController,
    private fb: FormBuilder,
    private toastController: ToastController
  ) {
    this.stageForm = this.fb.group({
      stageFrontendId: [''],
      stageId: [null],
      stageName: ['', [Validators.required, Validators.maxLength(50)]],
      order: [1, [Validators.required, Validators.min(1)]],
      recordStatus: ['New'], // Default to "New"
    });
  }

  ngOnInit(): void {
    if (this.stage) {
      this.stageForm.patchValue({
        stageFrontendId: this.stage.stageFrontendId || this.generateFrontendId(),
        stageId: this.stage.stageId ?? null,
        stageName: this.stage.stageName,
        order: this.stage.order ?? 1,
        recordStatus: this.stage.recordStatus ?? 'Uploaded',
      });
    } else {
      this.stageForm.patchValue({
        stageFrontendId: this.generateFrontendId(),
        stageName: '',
        order: 1,
        recordStatus: 'New',
      });
    }
  }  
  
  private generateFrontendId(): string {
    return 'S-' + Math.random().toString(36).substr(2, 9);
  }

  async saveStage(): Promise<void> {
    if (this.stageForm.invalid) {
      await this.showToast('Please provide a valid stage name!', 'danger');
      return;
    }

    const updatedStage: Stage = {
      stageFrontendId: this.stageForm.value.stageFrontendId,
      stageId: this.stageForm.value.stageId,
      stageName: this.stageForm.value.stageName.trim(),
      order: this.stageForm.value.order,
      recordStatus: this.isEditing
        ? (this.stage && (this.stage.stageName !== this.stageForm.value.stageName.trim() || this.stage.order !== this.stageForm.value.order)
            ? 'Update'
            : this.stageForm.value.recordStatus)
        : 'New',
    };

    await this.modalController.dismiss(updatedStage);
    await this.showToast(
      this.isEditing ? 'Stage updated successfully!' : 'New stage added successfully!',
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
}
