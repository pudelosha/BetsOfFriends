import { Component, Input, Output, EventEmitter } from '@angular/core';
import { IonicModule, ToastController } from '@ionic/angular';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, FormArray } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ModalController, AlertController } from '@ionic/angular';
import { EditUserModalComponent } from 'src/app/modals/edit-user-modal/edit-user-modal.component';

@Component({
  selector: 'app-stage-users-management',
  templateUrl: './stage-users-management.page.html',
  styleUrls: ['./stage-users-management.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule],
})
export class StageUsersManagementPage {
  @Input() usersArray!: FormArray; // Input from parent for users FormArray
  @Output() usersUpdated = new EventEmitter<any[]>(); // Emit current list of users

  constructor(
    private fb: FormBuilder, // Inject FormBuilder
    private toastController: ToastController,
    private modalController: ModalController,
    private alertController: AlertController
  ) {}

  // Get control for a specific user
  getUserControl(index: number): FormGroup {
    return this.usersArray.at(index) as FormGroup;
  }

  async addUser(): Promise<void> {
    const allUserEmails = this.usersArray.controls.map(control => control.get('userEmail')?.value.trim().toLowerCase());
  
    const modal = await this.modalController.create({
      component: EditUserModalComponent,
      componentProps: {
        user: {
          userId: null,
          userName: '',
          userAdminName: '',
          userEmail: '',
          status: 'New',
          userRole: 'Player',
        },
        isEditing: false,
        allUserEmails,
      },
    });
  
    modal.onDidDismiss().then(result => {
      if (result.data) {
        const newUser = {
          userId: null,
          userName: result.data.userName.trim(),
          userAdminName: result.data.userAdminName.trim(),
          userEmail: result.data.userEmail.trim().toLowerCase(),
          status: 'New',
          userRole: result.data.userRole,
          recordStatus: 'New'
        };
    
        this.usersArray.push(
          this.fb.group({
            userId: [newUser.userId],
            userName: [newUser.userName, Validators.required],
            userAdminName: [newUser.userAdminName, Validators.required],
            userEmail: [newUser.userEmail, [Validators.required, Validators.email]],
            status: [newUser.status, Validators.required],
            userRole: [newUser.userRole, Validators.required],
            recordStatus: [newUser.recordStatus]
          })
        );
    
        console.log('Added New User:', newUser);
        this.emitUsers();
      }
    });    
  
    await modal.present();
  }
  
  // Edit user
  async editUser(index: number): Promise<void> {
    const user = this.usersArray.at(index).value;

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
        const userGroup = this.usersArray.at(index) as FormGroup;
        const existingUser = userGroup.value;
    
        const isUpdated = existingUser.userName.trim() !== updatedUser.userName.trim() ||
                          existingUser.userAdminName.trim() !== updatedUser.userAdminName.trim() ||
                          existingUser.userEmail.trim().toLowerCase() !== updatedUser.userEmail.trim().toLowerCase() ||
                          existingUser.status !== updatedUser.status ||
                          existingUser.userRole !== updatedUser.userRole;
    
        userGroup.patchValue({
          userName: updatedUser.userName.trim(),
          userAdminName: updatedUser.userAdminName.trim(),
          userEmail: updatedUser.userEmail.trim().toLowerCase(),
          status: updatedUser.status,
          userRole: updatedUser.userRole,
          recordStatus: isUpdated ? 'Updated' : existingUser.recordStatus
        });
    
        console.log('Updated User:', updatedUser);
        this.emitUsers();
      }
    });
    
    await modal.present();
  }

  // Remove user
  async removeUser(index: number): Promise<void> {
    const userToRemove = this.usersArray.at(index).value;
  
    const alert = await this.alertController.create({
      header: 'Confirm Removal',
      message: `Are you sure you want to delete the user "${userToRemove.userName}"?`,
      buttons: [
        {
          text: 'Cancel',
          role: 'cancel',
        },
        {
          text: 'Delete',
          role: 'destructive',
          handler: async () => {
            if (userToRemove.recordStatus === 'New') {
              // If user is "New", remove it from the array
              this.usersArray.removeAt(index);
            } else {
              // Otherwise, mark it for deletion
              const userGroup = this.usersArray.at(index) as FormGroup;
              userGroup.patchValue({ recordStatus: 'Delete' });
            }
  
            this.emitUsers();
            console.log('Removed User:', userToRemove);
            await this.showToast(`User "${userToRemove.userName}" removed successfully!`, 'success');
          },
        },
      ],
    });
  
    await alert.present();
  }
  
  // Emit updated list of users to parent
  private emitUsers(): void {
    const updatedUsers = this.usersArray.value.map((user: any) => ({
      userId: user.userId,
      userName: user.userName,
      userAdminName: user.userAdminName,
      userEmail: user.userEmail,
      status: user.status,
      userRole: user.userRole,
      recordStatus: user.recordStatus ?? 'Uploaded'
    }));
  
    this.usersUpdated.emit(updatedUsers);
    console.log('Emitted Updated Users:', updatedUsers);
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
