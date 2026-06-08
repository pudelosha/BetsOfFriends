import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { ToastController } from '@ionic/angular';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, FormArray } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ModalController, AlertController } from '@ionic/angular';
import { EditUserModalComponent } from 'src/app/modals/edit-user-modal/edit-user-modal.component';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { IonList, IonItem, IonButton, IonIcon } from '@ionic/angular/standalone';
import { AuthService } from 'src/app/services/auth.service';

@Component({
  selector: 'app-stage-users-management',
  templateUrl: './stage-users-management.page.html',
  styleUrls: ['./stage-users-management.page.scss'],
  standalone: true,
  imports: [IonIcon, CommonModule, ReactiveFormsModule, TranslateModule, IonList, IonItem, IonButton],
})
export class StageUsersManagementPage implements OnInit {
  @Input() usersArray!: FormArray;
  @Output() usersUpdated = new EventEmitter<any[]>();

  isMobile = false;
  loggedInUserEmail: string = '';

  constructor(
    private fb: FormBuilder,
    private toastController: ToastController,
    private modalController: ModalController,
    private alertController: AlertController,
    private authService: AuthService,
    private translate: TranslateService
  ) {}

  ngOnInit(): void {
    this.checkScreenSize();
    window.addEventListener('resize', this.checkScreenSize.bind(this));

    this.loggedInUserEmail = (this.authService.getEmailFromToken())?.toLowerCase().trim() || '';
  }

  checkScreenSize(): void {
    this.isMobile = window.innerWidth < 600;
  }
  
  get filteredUserControls(): FormGroup[] {
    return this.usersArray.controls
      .filter(control => {
        const email = control.get('userEmail')?.value?.toLowerCase().trim();
        return email !== this.loggedInUserEmail;
      }) as FormGroup[];
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

  getUserControl(index: number): FormGroup {
    return this.usersArray.at(index) as FormGroup;
  }

  async addUser(): Promise<void> {
    const allUserEmails = this.usersArray.controls.map(control => control.get('userEmail')?.value.trim().toLowerCase());
  
    const modal = await this.modalController.create({
      component: EditUserModalComponent,
      componentProps: {
        user: {
          assignmentId: null,
          userName: '',
          userAdminName: '',
          userEmail: '',
          status: 'New',
          userRole: 'Player',
          recordStatus: 'New',
        },
        isEditing: false,
        allUserEmails,
      },
    });
  
    modal.onDidDismiss().then(result => {
      if (result.data) {
        const newUser = {
          assignmentId: null,
          userName: result.data.userName.trim(),
          userAdminName: result.data.userAdminName.trim(),
          userEmail: result.data.userEmail.trim().toLowerCase(),
          status: 'New',
          userRole: result.data.userRole,
          recordStatus: 'New',
        };
    
        this.usersArray.push(
          this.fb.group({
            assignmentId: [newUser.assignmentId],
            userName: [newUser.userName, Validators.required],
            userAdminName: [newUser.userAdminName, Validators.required],
            userEmail: [newUser.userEmail, [Validators.required, Validators.email]],
            status: [newUser.status, Validators.required],
            userRole: [newUser.userRole, Validators.required],
            recordStatus: [newUser.recordStatus]
          })
        );
    
        this.emitUsers();
      }
    });    
  
    await modal.present();
  }
  
  async editUser(userControl: FormGroup): Promise<void> {
    const user = userControl.value;
  
    const modal = await this.modalController.create({
      component: EditUserModalComponent,
      componentProps: {
        user,
        isEditing: true,
      },
    });
  
    modal.onDidDismiss().then(result => {
      if (result.data) {
        const updatedUser = result.data;
        const existingUser = userControl.value;
  
        const isUpdated =
          existingUser.userAdminName.trim() !== updatedUser.userAdminName.trim() ||
          existingUser.userEmail.trim().toLowerCase() !== updatedUser.userEmail.trim().toLowerCase() ||
          existingUser.status !== updatedUser.status ||
          existingUser.userRole !== updatedUser.userRole;
  
        userControl.patchValue({
          assignmentId: existingUser.assignmentId ?? null,
          userName: updatedUser.userName.trim(),
          userAdminName: updatedUser.userAdminName.trim(),
          userEmail: updatedUser.userEmail.trim().toLowerCase(),
          status: existingUser.status,
          userRole: updatedUser.userRole,
          recordStatus: isUpdated ? 'Update' : existingUser.recordStatus,
        });
  
        this.emitUsers();
      }
    });
  
    await modal.present();
  }  

  async removeUser(index: number): Promise<void> {
    const userToRemove = this.usersArray.at(index).value;
  
    const alert = await this.alertController.create({
      header: this.t('TOASTS.CONFIRM_REMOVAL_TITLE'),
      message: this.t('TOASTS.CONFIRM_REMOVE_USER', { name: userToRemove.userName }),
      buttons: [
        {
          text: this.t('TOASTS.CANCEL'),
          role: 'cancel',
        },
        {
          text: this.t('TOASTS.DELETE'),
          role: 'destructive',
          handler: async () => {
            if (userToRemove.recordStatus === 'New') {
              this.usersArray.removeAt(index);
            } else {
              const userGroup = this.usersArray.at(index) as FormGroup;
              userGroup.patchValue({ recordStatus: 'Delete' });
            }
  
            this.emitUsers();
            await this.showToast(this.t('TOASTS.USER_REMOVED', { name: userToRemove.userName }), 'success');
          },
        },
      ],
    });
  
    await alert.present();
  }
  
  private emitUsers(): void {
    const updatedUsers = this.usersArray.value.map((user: any) => ({
      assignmentId: user.assignmentId ?? null,
      userName: user.userName,
      userAdminName: user.userAdminName,
      userEmail: user.userEmail,
      status: user.status,
      userRole: user.userRole,
      recordStatus: user.recordStatus ?? 'Uploaded',
    }));
  
    this.usersUpdated.emit(updatedUsers);
  }  

getDeleteButtonText(recordStatus: string | null): string {
  return recordStatus === 'Delete' ? 'Undo' : 'Delete';
}

getDeleteButtonColor(recordStatus: string | null): string {
  return recordStatus === 'Delete' ? 'medium' : 'danger';
}

getUserStatusClass(recordStatus: string | null): string {
  switch (recordStatus) {
    case 'New': return 'user-status-new';
    case 'Update': return 'user-status-updated';
    case 'Delete': return 'user-status-delete';
    case 'Uploaded': return 'user-status-uploaded';
    default: return '';
  }
}

async handleRemoveOrUndoUser(userControl: FormGroup): Promise<void> {
  const userEmail = userControl.get('userEmail')?.value ?? 'this user';
  const currentStatus = userControl.get('recordStatus')?.value;

  if (currentStatus === 'Delete') {
    userControl.patchValue({ recordStatus: 'Update' });
    this.emitUsers();
    await this.showToast(this.t('TOASTS.USER_RESTORED', { name: userEmail }), 'success');
  } else {
    const alert = await this.alertController.create({
      header: this.t('TOASTS.CONFIRM_REMOVAL_TITLE'),
      message: this.t('TOASTS.CONFIRM_REMOVE_USER', { name: userEmail }),
      buttons: [
        { text: this.t('TOASTS.CANCEL'), role: 'cancel' },
        {
          text: this.t('TOASTS.DELETE'),
          role: 'destructive',
          handler: async () => {
            const index = this.usersArray.controls.indexOf(userControl);
            if (currentStatus === 'New') {
              this.usersArray.removeAt(index);
            } else {
              userControl.patchValue({ recordStatus: 'Delete' });
            }
            this.emitUsers();
            await this.showToast(this.t('TOASTS.USER_REMOVED', { name: userEmail }), 'success');
          },
        },
      ],
    });

    await alert.present();
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

private t(key: string, params?: Record<string, unknown>): string {
  return this.translate.instant(key, params);
}
}
