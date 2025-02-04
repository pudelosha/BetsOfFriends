import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { ToastController, AlertController, IonicModule } from '@ionic/angular';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, AbstractControl } from '@angular/forms';
import { UserService } from 'src/app/services/user.service';
import { UserProfile } from 'src/app/model/user-profile';
import { AuthService } from 'src/app/services/auth.service';

@Component({
  selector: 'app-profile',
  templateUrl: './profile.page.html',
  styleUrls: ['./profile.page.scss'],
  standalone: true,
  imports: [CommonModule, FormsModule, IonicModule, ReactiveFormsModule],
})
export class ProfilePage implements OnInit {
  profileForm: FormGroup;
  memberSince = '';
  isLoading = true;
  isUpdating = false;

  languages = [
    { value: 'en', label: 'English' },
    { value: 'pl', label: 'Polski' }
  ];

  constructor(
    private authService: AuthService,
    private fb: FormBuilder,
    private toastCtrl: ToastController,
    private alertCtrl: AlertController,
    private userService: UserService,
  ) {
    this.profileForm = this.fb.group({
      username: ['', [Validators.minLength(3), Validators.maxLength(50)]], 
      email: ['', [Validators.required, Validators.email]], 
      language: ['en'],
      darkMode: [false]
    });
  }

  ngOnInit() {
    this.loadUserProfile();
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
            if (!this.isValidEmail(data.newEmail)) {
              this.presentToast('Invalid email format.', 'warning');
              return false;
            }
  
            if (!this.isValidPassword(data.password)) {
              this.presentToast('Password must be at least 8 characters.', 'warning');
              return false;
            }
  
            try {
              await this.userService.changeEmail(data.newEmail, data.password).toPromise();
              this.presentToast('Email updated successfully!', 'success');
  
              this.profileForm.patchValue({ email: data.newEmail });
  
            } catch (error) {
              console.error('Error changing email:', error);
              this.presentToast('Failed to update email. Check your password.', 'danger');
            }
  
            return true;
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
            if (!this.isValidPassword(data.currentPassword)) {
              this.presentToast('Current password must be at least 8 characters.', 'warning');
              return false;
            }
            if (!this.isValidPassword(data.newPassword)) {
              this.presentToast('New password must be at least 8 characters.', 'warning');
              return false;
            }
            if (data.newPassword !== data.confirmPassword) {
              this.presentToast('Passwords do not match.', 'danger');
              return false;
            }
  
            try {
              await this.userService.updatePassword(data.currentPassword, data.newPassword).toPromise();
  
              this.authService.logout('Password updated successfully! Please log in again.');
  
            } catch (error) {
              console.error('Error updating password:', error);
              this.presentToast('Failed to update password. Check your current password.', 'danger');
            }
  
            return true;
          }
        }
      ]
    });
  
    await alert.present();
  }
  
  logoutUser() {
    this.authService.logout();
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

  loadUserProfile() {
    console.log('attempting to load user profile');
    this.isLoading = true;
  
    this.userService.getUserProfile().subscribe({
      next: (profile: UserProfile) => {
        console.log('User profile loaded:', profile);
  
        this.profileForm.patchValue({
          username: profile.username,
          email: profile.email,
          language: this.getLanguageValue(profile.language),
          darkMode: profile.darkMode
        });
  
        const dateObj = new Date(profile.memberSince);
        this.memberSince = `${dateObj.getFullYear()}.${(dateObj.getMonth() + 1).toString().padStart(2, '0')}.${dateObj.getDate().toString().padStart(2, '0')}`;
      },
      error: (error) => {
        console.error('Error loading profile:', error);
        this.presentToast('Failed to load profile.', 'danger');
      },
      complete: () => {
        this.isLoading = false;
      }
    });
  }
  
  
  onSubmitProfile() {
    if (this.profileForm.invalid) {
      this.presentToast('Please correct the errors before submitting.', 'danger');
      return;
    }

    this.isUpdating = true;

    const updatedProfile = {
      username: this.f['username'].value,
      language: this.f['language'].value,
      darkMode: this.f['darkMode'].value,
    };

    console.log('Updating profile:', updatedProfile);

    this.userService.updateUserProfile(updatedProfile).subscribe({
      next: () => {
        this.presentToast('Profile updated successfully!', 'success');
      },
      error: (error) => {
        console.error('Error updating profile:', error);
        this.presentToast('Failed to update profile. Please try again.', 'danger');
      },
      complete: () => {
        this.isUpdating = false;
      }
    });
  } 

  getLanguageValue(apiValue: string): string {
    const found = this.languages.find(lang => lang.label === apiValue || lang.value === apiValue);
    return found ? found.value : 'en';
  }  

  private isValidEmail(email: string): boolean {
    const emailPattern = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;
    return emailPattern.test(email);
  }
  
  private isValidPassword(password: string): boolean {
    return password.length >= 8;
  }  
}
