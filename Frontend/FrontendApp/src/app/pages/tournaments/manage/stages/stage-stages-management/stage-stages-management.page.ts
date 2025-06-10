import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { ToastController } from '@ionic/angular';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, FormArray } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ModalController, AlertController } from '@ionic/angular';
import { EditStageModalComponent } from 'src/app/modals/edit-stage-modal/edit-stage-modal.component';
import { Stage } from 'src/app/model/tournament-model';
import { TranslateModule } from '@ngx-translate/core';
import { IonList, IonItem, IonButton, IonIcon } from '@ionic/angular/standalone';

@Component({
  selector: 'app-stage-stages-management',
  templateUrl: './stage-stages-management.page.html',
  styleUrls: ['./stage-stages-management.page.scss'],
  standalone: true,
  imports: [IonIcon, CommonModule, ReactiveFormsModule, TranslateModule, IonList, IonItem, IonButton],
})
export class StageStagesManagementPage implements OnInit {
  @Input() stagesArray!: FormArray;
  @Output() stagesUpdated = new EventEmitter<{ previousStages: Stage[]; updatedStages: Stage[] }>();

  isMobile = false;

  constructor(
    private toastController: ToastController,
    private modalController: ModalController,
    private alertController: AlertController,
    private fb: FormBuilder
  ) {}

  ngOnInit(): void {
    this.checkScreenSize();
    window.addEventListener('resize', this.checkScreenSize.bind(this));
  }

  getDeleteIcon(status: string): string {
    switch (status) {
      case 'Delete': return 'arrow-undo-outline';
      case 'Uploaded':
      case 'Update':
      case 'New':
      default:
        return 'trash-outline';
    }
  }

  getStageControl(index: number): FormGroup {
    return this.stagesArray.at(index) as FormGroup;
  }

  checkScreenSize(): void {
    this.isMobile = window.innerWidth < 600;
  }

  private generateFrontendId(): string {
    return 'S-' + Math.random().toString(36).substr(2, 9);
  }

  async addStage(): Promise<void> {
    const existingOrders = this.stagesArray.controls.map(control => control.get('order')?.value);
    const nextOrder = existingOrders.length > 0 ? Math.max(...existingOrders) + 1 : 1;
  
    const modal = await this.modalController.create({
      component: EditStageModalComponent,
      componentProps: {
        stage: { 
          stageFrontendId: this.generateFrontendId(),
          stageId: null,
          stageName: '',
          order: nextOrder,
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

  async handleRemoveOrUndoStage(index: number): Promise<void> {
    const stageControl = this.getStageControl(index);
    const stageToRemove = stageControl.value;
    const currentStatus = stageToRemove.recordStatus;

    if (currentStatus === 'Delete') {
      stageControl.patchValue({ recordStatus: 'Update' });
      this.emitStages();
      await this.showToast(`Stage "${stageToRemove.stageName}" restored successfully!`, 'success');
    } else {
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

  getDeleteButtonText(recordStatus: string | null): string {
    return recordStatus === 'Delete' ? 'Undo' : 'Delete';
  }

  getDeleteButtonColor(recordStatus: string | null): string {
    return recordStatus === 'Delete' ? 'medium' : 'danger';
  }  

  private insertStageWithOrder(newStage: Stage): void {
    const existingOrders = this.stagesArray.controls.map(control => control.get('order')?.value);
    
    if (existingOrders.includes(newStage.order)) {
      this.stagesArray.controls.forEach(control => {
        const currentOrder = control.get('order')?.value;
        if (currentOrder >= newStage.order) {
          control.patchValue({ order: currentOrder + 1 });
        }
      });
    }

    this.stagesArray.push(this.fb.group(newStage));
    this.recalculateStageOrder();
    this.emitStages();
  }

  private recalculateStageOrder(): void {
    this.stagesArray.controls
      .sort((a, b) => a.get('order')!.value - b.get('order')!.value)
      .forEach((control, index) => {
        (control as FormGroup).patchValue({ order: index + 1 });
      });
  }

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
