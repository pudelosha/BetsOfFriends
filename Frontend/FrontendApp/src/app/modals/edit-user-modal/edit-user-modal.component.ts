import { Component, Input, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { ModalController, ToastController, IonicModule} from '@ionic/angular';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-edit-user-modal',
  templateUrl: './edit-user-modal.component.html',
  styleUrls: ['./edit-user-modal.component.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule],
})
export class EditUserModalComponent implements OnInit {
  @Input() user: {
    userId: string | null;
    userName: string;
    userAdminName: string;
    userEmail: string;
    status: 'New' | 'Invited' | 'Accepted';
    userRole: 'Player' | 'Admin';
    recordStatus: string;
  } | null = null;

  @Input() isEditing: boolean = false;

  userForm: FormGroup;

  constructor(
    private modalController: ModalController,
    private fb: FormBuilder,
    private toastController: ToastController
  ) {
    this.userForm = this.fb.group({
      userName: [{ value: '', disabled: true }], // Non-editable
      userAdminName: ['', Validators.required], // Editable anytime
      userEmail: ['', [Validators.required, Validators.email]], // Conditionally editable
      userRole: ['Player', Validators.required], // Default to Player
      status: [{ value: 'New', disabled: true }], // Always display-only
      recordStatus: ['New'], // Track record status
    });
  }

  ngOnInit(): void {
    if (this.user) {
      this.userForm.patchValue({
        ...this.user,
        recordStatus: this.user.recordStatus ?? 'Uploaded' // Default to "Uploaded"
      });
  
      // Disable email input if the user has been invited or accepted
      if (this.user.status !== 'New') {
        this.userForm.get('userEmail')?.disable();
      }
  
      // Ensure "New" status cannot be changed
      this.userForm.get('status')?.disable();
    } else {
      this.userForm.patchValue({
        recordStatus: 'New' // Default for new users
      });
    }
  }
  
  async saveUser(): Promise<void> {
    if (this.userForm.invalid) {
      await this.showToast('Please provide valid user details!', 'danger');
      return;
    }
  
    const existingUser = this.user;
    const updatedUser = this.userForm.getRawValue();
  
    // Check if a real change happened
    const isUpdated = existingUser &&
      (existingUser.userAdminName !== updatedUser.userAdminName ||
      existingUser.userEmail !== updatedUser.userEmail ||
      existingUser.userRole !== updatedUser.userRole);
  
    // Preserve existing values but update recordStatus
    const finalUser = {
      ...existingUser, // Preserve existing user fields
      ...updatedUser,  // Apply new form values
      status: existingUser?.status ?? 'New', // Ensure status is preserved
      recordStatus: this.isEditing
        ? (isUpdated ? 'Update' : existingUser?.recordStatus) // Mark "Update" only if changed
        : 'New', // Default for new users
    };
  
    await this.modalController.dismiss(finalUser);
    await this.showToast(
      this.isEditing ? 'User updated successfully!' : 'New user added successfully!',
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
