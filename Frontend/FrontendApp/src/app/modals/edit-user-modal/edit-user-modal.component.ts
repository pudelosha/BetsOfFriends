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
      status: [{ value: 'New', disabled: true }], // Always display-only
    });
  }

  ngOnInit(): void {
    if (this.user) {
      this.userForm.patchValue(this.user);

      // Handle conditional logic for userEmail editability
      if (this.user.status !== 'New') {
        this.userForm.get('userEmail')?.disable(); // Disable email for "Invited" and "Accepted" statuses
      }

      // Ensure "New" status cannot be changed
      this.userForm.get('status')?.disable();
    } else {
      // Default values for adding a new user
      this.userForm.get('status')?.setValue('New');
      this.userForm.get('status')?.disable(); // Fixed status for new users
    }
  }

  async saveUser(): Promise<void> {
    if (this.userForm.invalid) {
      await this.showToast('Please provide valid user details!', 'danger');
      return;
    }
  
    // Get form values and ensure status is included
    const updatedUser = {
      ...this.user, // Preserve existing user fields
      ...this.userForm.getRawValue(), // Get all form values, including disabled ones
      status: this.user?.status ?? 'New', // Ensure status is included
    };
  
    await this.modalController.dismiss(updatedUser);
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
