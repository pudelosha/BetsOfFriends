import { Component, Input, Output, EventEmitter } from '@angular/core';
import { ToastController } from '@ionic/angular';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, FormArray } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ModalController, AlertController } from '@ionic/angular';
import { EditStageModalComponent } from 'src/app/modals/edit-stage-modal/edit-stage-modal.component';
import { Stage } from 'src/app/model/tournament-model';
import { TranslateModule } from '@ngx-translate/core';
import { IonList, IonItem, IonButton } from '@ionic/angular/standalone';

@Component({
  selector: 'app-stage-stages-management',
  templateUrl: './stage-stages-management.page.html',
  styleUrls: ['./stage-stages-management.page.scss'],
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TranslateModule, IonList, IonItem, IonButton],
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
          recordStatus: 'New'
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
      const currentStage = stageGroup.value;
    
      // Only update status if something has changed
      if (updatedStage.stageName.trim() !== currentStage.stageName.trim() || updatedStage.order !== currentStage.order) {
        stageGroup.patchValue({
          stageName: updatedStage.stageName.trim(),
          order: updatedStage.order,
          recordStatus: 'Update'
        });
      }
    
      this.emitStages();
    });

    await modal.present();
  }

  // Remove a stage and update order
  async handleRemoveOrUndoStage(index: number): Promise<void> {
    const stageControl = this.getStageControl(index);
    const stageToRemove = stageControl.value;
    const currentStatus = stageToRemove.recordStatus;

    if (currentStatus === 'Delete') {
      // If already marked "Delete", undo by setting it to "Update"
      stageControl.patchValue({ recordStatus: 'Update' });
      this.emitStages();
      await this.showToast(`Stage "${stageToRemove.stageName}" restored successfully!`, 'success');
    } else {
      // Otherwise, show confirmation alert before deleting or marking as "Delete"
      const alert = await this.alertController.create({
        header: 'Confirm Removal',
        message: `Are you sure you want to delete the stage "${stageToRemove.stageName}"?`,
        buttons: [
          { text: 'Cancel', role: 'cancel' },
          {
            text: 'Delete',
            role: 'destructive',
            handler: async () => {
              if (currentStatus === 'New') {
                this.stagesArray.removeAt(index);
              } else {
                stageControl.patchValue({ recordStatus: 'Delete' });
              }
              this.emitStages();
              await this.showToast(`Stage "${stageToRemove.stageName}" removed successfully!`, 'success');
            },
          },
        ],
      });

      await alert.present();
    }
  } 

  // Determines Delete vs Undo button text
  getDeleteButtonText(recordStatus: string | null): string {
    return recordStatus === 'Delete' ? 'Undo' : 'Delete';
  }

  // Determines button color based on record status
  getDeleteButtonColor(recordStatus: string | null): string {
    return recordStatus === 'Delete' ? 'medium' : 'danger';
  }  

  // Insert a stage and handle conflicting orders
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
      recordStatus: stage.recordStatus ?? 'Uploaded'
    }));

    this.stagesUpdated.emit({ previousStages: updatedStages, updatedStages });
  }

  getRecordStatusClass(recordStatus: string | null): string {
    switch (recordStatus) {
      case 'New': return 'stage-status-new';
      case 'Update': return 'stage-status-updated';
      case 'Delete': return 'stage-status-delete';
      case 'Uploaded': return 'stage-status-uploaded';
      default: return '';
    }
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
