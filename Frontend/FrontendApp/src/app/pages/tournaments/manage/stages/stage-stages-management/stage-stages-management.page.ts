import { Component, Input, Output, EventEmitter } from '@angular/core';
import { IonicModule, ToastController } from '@ionic/angular';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, FormArray } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ModalController, AlertController } from '@ionic/angular';
import { EditStageModalComponent } from 'src/app/modals/edit-stage-modal/edit-stage-modal.component';
import { Stage } from 'src/app/model/tournament-model';

@Component({
  selector: 'app-stage-stages-management',
  templateUrl: './stage-stages-management.page.html',
  styleUrls: ['./stage-stages-management.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule],
})
export class StageStagesManagementPage {
  @Input() stagesArray!: FormArray;
  @Output() stagesUpdated = new EventEmitter<{ previousStages: Stage[]; updatedStages: Stage[] }>();

  constructor(
    private toastController: ToastController,
    private modalController: ModalController,
    private alertController: AlertController,
    private fb: FormBuilder
  ) {}

  // Get control for a specific stage
  getStageControl(index: number): FormGroup {
    return this.stagesArray.at(index) as FormGroup;
  }

  // Generate a unique frontendId for new stages
  private generateFrontendId(): string {
    return 'S-' + Math.random().toString(36).substr(2, 9);
  }

  // Add a new stage with proper order handling
  async addStage(): Promise<void> {
    const existingOrders = this.stagesArray.controls.map(control => control.get('order')?.value);
    const nextOrder = existingOrders.length > 0 ? Math.max(...existingOrders) + 1 : 1; // Get max + 1, default to 1 if empty
  
    const modal = await this.modalController.create({
      component: EditStageModalComponent,
      componentProps: {
        stage: { 
          stageFrontendId: this.generateFrontendId(),
          stageId: null,
          stageName: '',
          order: nextOrder, // Pre-fill order with max order + 1
        },
        isEditing: false,
      },
    });
  
    modal.onDidDismiss().then(result => {
      if (result.data) {
        const newStage: Stage = {
          stageFrontendId: this.generateFrontendId(),
          stageId: null,
          stageName: result.data.stageName.trim(),
          order: result.data.order,
        };
  
        this.insertStageWithOrder(newStage);
      }
    });
  
    await modal.present();
  }  

  // Edit an existing stage
  async editStage(index: number): Promise<void> {
    const stage = this.stagesArray.at(index).value;

    const modal = await this.modalController.create({
      component: EditStageModalComponent,
      componentProps: {
        stage,
        isEditing: true,
      },
    });

    modal.onDidDismiss().then(result => {
      if (!result.data) {
        console.warn('No data returned from modal.');
        return;
      }

      const updatedStage = result.data;
      const stageGroup = this.stagesArray.at(index) as FormGroup;
      stageGroup.patchValue({
        stageName: updatedStage.stageName.trim(),
        order: updatedStage.order,
      });

      this.emitStages();
    });

    await modal.present();
  }

  // Remove a stage and update order
  async removeStage(index: number): Promise<void> {
    const stageToRemove = this.stagesArray.at(index).value;

    const alert = await this.alertController.create({
      header: 'Confirm Deletion',
      message: `Are you sure you want to delete the stage "${stageToRemove.stageName}"?`,
      buttons: [
        {
          text: 'Cancel',
          role: 'cancel',
        },
        {
          text: 'Delete',
          role: 'destructive',
          handler: async () => {
            this.stagesArray.removeAt(index);
            this.recalculateStageOrder();
            this.emitStages();
          },
        },
      ],
    });

    await alert.present();
  }

  // 🆕 Insert a stage and handle conflicting orders
  private insertStageWithOrder(newStage: Stage): void {
    const existingOrders = this.stagesArray.controls.map(control => control.get('order')?.value);
    
    if (existingOrders.includes(newStage.order)) {
      // If order exists, shift existing stages down
      this.stagesArray.controls.forEach(control => {
        const currentOrder = control.get('order')?.value;
        if (currentOrder >= newStage.order) {
          control.patchValue({ order: currentOrder + 1 });
        }
      });
    }

    // Add the new stage at the correct position
    this.stagesArray.push(this.fb.group(newStage));

    // Ensure the order is still valid
    this.recalculateStageOrder();
    this.emitStages();
  }

  // Fix stage ordering after changes
  private recalculateStageOrder(): void {
    this.stagesArray.controls
      .sort((a, b) => a.get('order')!.value - b.get('order')!.value) // Ensure sorted order
      .forEach((control, index) => {
        (control as FormGroup).patchValue({ order: index + 1 });
      });
  }

  // Emit updated stages to parent
  private emitStages(): void {
    const updatedStages: Stage[] = this.stagesArray.value.map((stage: any) => ({
      stageFrontendId: stage.stageFrontendId,
      stageId: stage.stageId,
      stageName: stage.stageName,
      order: stage.order,
    }));

    this.stagesUpdated.emit({ previousStages: updatedStages, updatedStages });
  }

  // Show toast messages
  private async showToast(message: string, color: 'success' | 'warning' | 'danger' | 'primary'): Promise<void> {
    const toast = await this.toastController.create({
      message,
      duration: 3000,
      position: 'bottom',
      color,
    });
    await toast.present();
  }
}
