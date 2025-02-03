import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { ToastController, AlertController, IonicModule } from '@ionic/angular';
import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators, AbstractControl } from '@angular/forms';

@Component({
  selector: 'app-profile',
  templateUrl: './profile.page.html',
  styleUrls: ['./profile.page.scss'],
  standalone: true,
  imports: [CommonModule, FormsModule, IonicModule, ReactiveFormsModule],
})
export class ProfilePage {
  profileForm: FormGroup;
  memberSince = '2023-05-10'; // Mocked for now, should be fetched from API

  constructor(
    private fb: FormBuilder,
    private toastCtrl: ToastController,
    private alertCtrl: AlertController,
  ) {
    this.profileForm = this.fb.group({
      username: ['', [Validators.minLength(3), Validators.maxLength(50)]], 
      email: ['johndoe@example.com', [Validators.required, Validators.email]], 
      selectedLanguage: ['en'],
      darkMode: [false]
    });
  }

  get f(): { [key: string]: AbstractControl } {
    return this.profileForm.controls;
  }

  async presentToast(message: string, color: string) {
    const toast = await this.toastCtrl.create({
      message,
      duration: 3000,
      color: color,
      position: 'bottom',
      cssClass: color === 'danger' ? 'error-toast' : ''
    });
    await toast.present();
  }

  async changeEmail() {
    const alert = await this.alertCtrl.create({
      header: 'Change Email',
      inputs: [
        { name: 'newEmail', type: 'email', placeholder: 'Enter new email' },
        { name: 'password', type: 'password', placeholder: 'Enter password' }
      ],
      buttons: [
        { text: 'Cancel', role: 'cancel' },
        {
          text: 'Change',
          handler: async (data) => {
            if (data.newEmail && data.password) {
              // TODO: Call API to verify email change
              this.presentToast('Verification email sent.', 'success');
            } else {
              this.presentToast('Please enter valid details.', 'warning');
            }
          }
        }
      ]
    });
    await alert.present();
  }

  async updatePasswordPopup() {
    const alert = await this.alertCtrl.create({
      header: 'Update Password',
      inputs: [
        { name: 'currentPassword', type: 'password', placeholder: 'Enter current password' },
        { name: 'newPassword', type: 'password', placeholder: 'Enter new password' },
        { name: 'confirmPassword', type: 'password', placeholder: 'Confirm new password' }
      ],
      buttons: [
        { text: 'Cancel', role: 'cancel' },
        {
          text: 'Update',
          handler: async (data) => {
            if (!data.currentPassword || !data.newPassword || !data.confirmPassword) {
              this.presentToast('All fields are required.', 'warning');
              return false;
            }
            if (data.newPassword !== data.confirmPassword) {
              this.presentToast('Passwords do not match.', 'danger');
              return false;
            }

            // TODO: Call API to update password
            this.presentToast('Password updated successfully.', 'success');
            return true;
          }
        }
      ]
    });
    await alert.present();
  }

  async confirmDeleteAccount() {
    const alert = await this.alertCtrl.create({
      header: 'Delete Account',
      message: 'Are you sure you want to delete your account? This action is irreversible.',
      inputs: [
        { name: 'password', type: 'password', placeholder: 'Enter password' }
      ],
      buttons: [
        { text: 'Cancel', role: 'cancel' },
        {
          text: 'Delete',
          cssClass: 'danger-button',
          handler: async (data) => {
            if (data.password) {
              // TODO: Call API to mark the account for deletion
              this.presentToast('Your account has been marked for deletion.', 'danger');
            } else {
              this.presentToast('Please enter your password.', 'warning');
            }
          }
        }
      ]
    });
    await alert.present();
  }

  onSubmitProfile() {
    // TODO: Implement API call to save username, language, dark mode
    console.log('updating profile');
    this.presentToast('Profile updated successfully!', 'success');
  }
}
