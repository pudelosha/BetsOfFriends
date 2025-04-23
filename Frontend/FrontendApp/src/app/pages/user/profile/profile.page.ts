import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { ToastController, AlertController, LoadingController, ModalController } from '@ionic/angular';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, AbstractControl } from '@angular/forms';
import { UserService } from 'src/app/services/user.service';
import { UserProfile } from 'src/app/model/user-profile';
import { AuthService } from 'src/app/services/auth.service';
import { Country } from 'src/app/model/location';
import { LocationService } from 'src/app/services/location.service';
import { firstValueFrom } from 'rxjs';
import { LanguageService } from 'src/app/services/language.service';
import { Language } from 'src/app/model/language';
import { TranslateModule } from '@ngx-translate/core';
import { TitleService } from 'src/app/services/title.service';
import { IonContent, IonItem, IonLabel, IonText, IonInput, IonSelect, IonSelectOption, IonToggle, IonButton, IonSpinner } from '@ionic/angular/standalone';
import { SelectCountryModalComponent } from 'src/app/modals/select-country-modal/select-country-modal.component';

@Component({
  selector: 'app-profile',
  templateUrl: './profile.page.html',
  styleUrls: ['./profile.page.scss'],
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TranslateModule, IonContent, IonText, IonItem, IonLabel, IonInput, IonSelect, IonSelectOption, IonToggle, IonButton, IonSpinner]
})
export class ProfilePage implements OnInit {
  profileForm: FormGroup;
  memberSince = '';
  isLoading = true;
  isUpdating = false;
  availableCountries: Country[] = [];
  availableLanguages: Language[] = [];

  constructor(
    private authService: AuthService,
    private fb: FormBuilder,
    private toastCtrl: ToastController,
    private alertCtrl: AlertController,
    private userService: UserService,
    private modalController: ModalController,
    private locationService: LocationService,
    private loadingController: LoadingController,
    private languageService: LanguageService,
    private titleService: TitleService
  ) {
    this.profileForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      nickname: [''],
      language: ['en'],
      location: [''],
      darkMode: [false]
    });    
  }

  ngOnInit() {
    this.titleService.setTitle('PROFILE.TITLE');
  }

  ionViewWillEnter() {
    this.titleService.setTitle('PROFILE.TITLE');
    this.loadCountries();
    this.loadLanguages();
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

  async loadCountries() {
    try {
      this.availableCountries = await firstValueFrom(this.locationService.getAvailableCountries());
    } catch (error) {
      console.error('Failed to load countries:', error);
      this.presentToast('Could not load countries list.', 'danger');
    }
  }

  async loadLanguages() {
    try {
      this.availableLanguages = await firstValueFrom(this.languageService.getAvailableLanguages());
    } catch (error) {
      console.error('Failed to load languages:', error);
      this.presentToast('Could not load languages list.', 'danger');
    }
  }

  async openCountrySelector() {
    const modal = await this.modalController.create({
      component: SelectCountryModalComponent,
      componentProps: {
        countries: this.availableCountries // provide your array of countries
      }
    });
  
    await modal.present();
  
    const { data } = await modal.onDidDismiss();
    if (data !== undefined) {
      this.profileForm.controls['location'].setValue(data);
    }
  }

  getSelectedCountryName(): string | null {
    const id = this.profileForm.controls['location'].value;
    return this.availableCountries.find(c => c.countryId === id)?.name ?? null;
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
              await firstValueFrom(this.userService.changeEmail(data.newEmail, data.password));
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
              await firstValueFrom(this.userService.updatePassword(data.currentPassword, data.newPassword));
  
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
            if (!this.isValidPassword(data.password)) {
              await this.presentToast('Password must be at least 8 characters.', 'warning');
              return false;
            }
  
            const loading = await this.loadingController.create({
              message: 'Deleting account...',
              spinner: 'crescent',
            });
            await loading.present();
  
            const startTime = Date.now();
  
            try {
              await firstValueFrom(this.userService.deleteAccount(data.password));
              await this.authService.logout('Your account has been deleted. We hope to see you again!', '/register');
            } catch (error) {
              console.error('Error deleting account:', error);
              await this.presentToast('Failed to delete account. Check your password.', 'danger');
            } finally {
              const elapsedTime = Date.now() - startTime;
              const delay = Math.max(0, 500 - elapsedTime);
  
              setTimeout(async () => {
                await loading.dismiss();
              }, delay);
            }
  
            return true;
          }
        }
      ]
    });
  
    await alert.present();
  }
    
  async loadUserProfile() {
    //console.log('Attempting to load user profile');
    this.isLoading = true;
  
    const loading = await this.loadingController.create({
      message: 'Loading profile...',
      spinner: 'crescent',
    });
    await loading.present();
  
    const startTime = Date.now(); // Start time for delay calculation
  
    this.userService.getUserProfile().subscribe({
      next: (profile: UserProfile) => {
        //console.log('User profile loaded:', profile);
  
        this.profileForm.patchValue({
          email: profile.email,
          nickname: profile.nickname ?? '',
          location: profile.location?.countryId ?? '',
          language: profile.language ?? '',
          darkMode: profile.darkMode
        });      
  
        const dateObj = new Date(profile.memberSince);
        this.memberSince = `${dateObj.getFullYear()}.${(dateObj.getMonth() + 1).toString().padStart(2, '0')}.${dateObj.getDate().toString().padStart(2, '0')}`;
      },
      error: async (error) => {
        console.error('Error loading profile:', error);
        await this.presentToast('Failed to load profile.', 'danger');
      },
      complete: async () => {
        const elapsedTime = Date.now() - startTime;
        const delay = Math.max(0, 500 - elapsedTime); // Ensure at least 500ms delay
  
        setTimeout(async () => {
          this.isLoading = false;
          await loading.dismiss();
        }, delay);
      }
    });
  } 
  
  async onSubmitProfile() {
    if (this.profileForm.invalid) {
      await this.presentToast('Please correct the errors before submitting.', 'danger');
      return;
    }
  
    this.isUpdating = true;
  
    const selectedCountry = this.availableCountries.find(
      c => c.countryId === Number(this.f['location'].value)
    );
  
    const selectedLanguage = this.availableLanguages.find(
      l => l.shortName === this.f['language'].value
    );
  
    const langCode = selectedLanguage?.shortName ?? 'en';
  
    const updatedProfile = {
      nickname: this.f['nickname'].value?.trim() || null,
      location: selectedCountry
        ? { countryId: selectedCountry.countryId, name: selectedCountry.name }
        : null,
      language: langCode,
      darkMode: this.f['darkMode'].value
    };
  
    //console.log('Updating profile:', updatedProfile);
  
    const loading = await this.loadingController.create({
      message: 'Updating profile...',
      spinner: 'crescent',
    });
    await loading.present();
  
    const startTime = Date.now();
  
    this.userService.updateUserProfile(updatedProfile).subscribe({
      next: async () => {
        this.languageService.useLanguage(langCode);
        await this.presentToast('Profile updated successfully!', 'success');
      },
      error: async (error) => {
        console.error('Error updating profile:', error);
        await this.presentToast('Failed to update profile. Please try again.', 'danger');
      },
      complete: async () => {
        const elapsedTime = Date.now() - startTime;
        const delay = Math.max(0, 500 - elapsedTime);
  
        setTimeout(async () => {
          this.isUpdating = false;
          await loading.dismiss();
        }, delay);
      }
    });
  }
    
  getLanguageValue(apiValue: string): string {
    const found = this.availableLanguages.find(lang => lang.shortName === apiValue);
    return found ? found.shortName : 'en';
  }

  private isValidEmail(email: string): boolean {
    const emailPattern = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;
    return emailPattern.test(email);
  }
  
  private isValidPassword(password: string): boolean {
    return password.length >= 8;
  }  
}
