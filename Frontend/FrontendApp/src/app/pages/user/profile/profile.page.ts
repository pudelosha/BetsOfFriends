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
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { TitleService } from 'src/app/services/title.service';
import { IonContent, IonItem, IonLabel, IonInput, IonSelect, IonSelectOption, IonToggle, IonButton, IonSpinner } from '@ionic/angular/standalone';
import { SelectCountryModalComponent } from 'src/app/modals/select-country-modal/select-country-modal.component';
import { BackendMessageService } from 'src/app/services/backend-message.service';

@Component({
  selector: 'app-profile',
  templateUrl: './profile.page.html',
  styleUrls: ['./profile.page.scss'],
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TranslateModule, IonContent, IonItem, IonLabel, IonInput, IonSelect, IonSelectOption, IonToggle, IonButton, IonSpinner]
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
    private titleService: TitleService,
    private translate: TranslateService,
    private backendMessages: BackendMessageService
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
      this.presentToast(this.t('PROFILE.LOAD_COUNTRIES_FAILED'), 'danger');
    }
  }

  async loadLanguages() {
    try {
      this.availableLanguages = await firstValueFrom(this.languageService.getAvailableLanguages());
    } catch (error) {
      console.error('Failed to load languages:', error);
      this.presentToast(this.t('PROFILE.LOAD_LANGUAGES_FAILED'), 'danger');
    }
  }

  async openCountrySelector() {
    const modal = await this.modalController.create({
      component: SelectCountryModalComponent,
      componentProps: {
        countries: this.availableCountries
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
      header: this.t('PROFILE.CHANGE_EMAIL'),
      inputs: [
        { name: 'newEmail', type: 'email', placeholder: this.t('PROFILE.NEW_EMAIL_PLACEHOLDER') },
        { name: 'password', type: 'password', placeholder: this.t('PROFILE.PASSWORD_PLACEHOLDER') }
      ],
      buttons: [
        { text: this.t('PROFILE.CANCEL'), role: 'cancel' },
        {
          text: this.t('PROFILE.CHANGE'),
          handler: async (data) => {
            if (!this.isValidEmail(data.newEmail)) {
              this.presentToast(this.t('PROFILE.INVALID_EMAIL'), 'warning');
              return false;
            }
  
            if (!this.isValidPassword(data.password)) {
              this.presentToast(this.t('PROFILE.PASSWORD_MIN_LENGTH'), 'warning');
              return false;
            }
  
            try {
              await firstValueFrom(this.userService.changeEmail(data.newEmail, data.password));
              this.presentToast(this.t('PROFILE.EMAIL_UPDATED'), 'success');
  
              this.profileForm.patchValue({ email: data.newEmail });
  
            } catch (error) {
              console.error('Error changing email:', error);
              this.presentToast(this.backendMessages.translateMessage(this.extractBackendMessage(error), 'PROFILE.EMAIL_UPDATE_FAILED'), 'danger');
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
      header: this.t('PROFILE.UPDATE_PASSWORD'),
      inputs: [
        { name: 'currentPassword', type: 'password', placeholder: this.t('PROFILE.CURRENT_PASSWORD_PLACEHOLDER') },
        { name: 'newPassword', type: 'password', placeholder: this.t('PROFILE.NEW_PASSWORD_PLACEHOLDER') },
        { name: 'confirmPassword', type: 'password', placeholder: this.t('PROFILE.CONFIRM_PASSWORD_PLACEHOLDER') }
      ],
      buttons: [
        { text: this.t('PROFILE.CANCEL'), role: 'cancel' },
        {
          text: this.t('PROFILE.UPDATE'),
          handler: async (data) => {
            if (!this.isValidPassword(data.currentPassword)) {
              this.presentToast(this.t('PROFILE.CURRENT_PASSWORD_MIN_LENGTH'), 'warning');
              return false;
            }
            if (!this.isValidPassword(data.newPassword)) {
              this.presentToast(this.t('PROFILE.NEW_PASSWORD_MIN_LENGTH'), 'warning');
              return false;
            }
            if (data.newPassword !== data.confirmPassword) {
              this.presentToast(this.t('PROFILE.PASSWORDS_DO_NOT_MATCH'), 'danger');
              return false;
            }
  
            try {
              await firstValueFrom(this.userService.updatePassword(data.currentPassword, data.newPassword));
  
              this.authService.logout(this.t('PROFILE.PASSWORD_UPDATED_LOGOUT'));
  
            } catch (error) {
              console.error('Error updating password:', error);
              this.presentToast(this.backendMessages.translateMessage(this.extractBackendMessage(error), 'PROFILE.PASSWORD_UPDATE_FAILED'), 'danger');
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
      header: this.t('PROFILE.DELETE_ACCOUNT'),
      message: this.t('PROFILE.DELETE_ACCOUNT_CONFIRMATION'),
      inputs: [
        { name: 'password', type: 'password', placeholder: this.t('PROFILE.PASSWORD_PLACEHOLDER') }
      ],
      buttons: [
        { text: this.t('PROFILE.CANCEL'), role: 'cancel' },
        {
          text: this.t('PROFILE.DELETE'),
          cssClass: 'danger-button',
          handler: async (data) => {
            if (!this.isValidPassword(data.password)) {
              await this.presentToast(this.t('PROFILE.PASSWORD_MIN_LENGTH'), 'warning');
              return false;
            }
  
            const loading = await this.loadingController.create({
              message: this.t('PROFILE.DELETING_ACCOUNT'),
              spinner: 'crescent',
            });
            await loading.present();
  
            const startTime = Date.now();
  
            try {
              await firstValueFrom(this.userService.deleteAccount(data.password));
              await this.authService.logout(this.t('PROFILE.ACCOUNT_DELETED_LOGOUT'), '/register');
            } catch (error) {
              console.error('Error deleting account:', error);
              await this.presentToast(this.backendMessages.translateMessage(this.extractBackendMessage(error), 'PROFILE.ACCOUNT_DELETE_FAILED'), 'danger');
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
    this.isLoading = true;
  
    const loading = await this.loadingController.create({
      message: this.t('PROFILE.LOADING_PROFILE'),
      spinner: 'crescent',
    });
    await loading.present();
  
    const startTime = Date.now();
  
    this.userService.getUserProfile().subscribe({
      next: (profile: UserProfile) => {
  
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
        await this.presentToast(this.t('PROFILE.LOAD_PROFILE_FAILED'), 'danger');
      },
      complete: async () => {
        const elapsedTime = Date.now() - startTime;
        const delay = Math.max(0, 500 - elapsedTime);
  
        setTimeout(async () => {
          this.isLoading = false;
          await loading.dismiss();
        }, delay);
      }
    });
  } 
  
  async onSubmitProfile() {
    if (this.profileForm.invalid) {
      await this.presentToast(this.t('PROFILE.SUBMIT_ERRORS'), 'danger');
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
  
    const loading = await this.loadingController.create({
      message: this.t('PROFILE.UPDATING_PROFILE'),
      spinner: 'crescent',
    });
    await loading.present();
  
    const startTime = Date.now();
  
    this.userService.updateUserProfile(updatedProfile).subscribe({
      next: async () => {
        this.languageService.useLanguage(langCode);
        await this.presentToast(this.t('PROFILE.PROFILE_UPDATED'), 'success');
      },
      error: async (error) => {
        console.error('Error updating profile:', error);
        await this.presentToast(this.backendMessages.translateMessage(this.extractBackendMessage(error), 'PROFILE.PROFILE_UPDATE_FAILED'), 'danger');
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

  private t(key: string, params?: Record<string, unknown>): string {
    return this.translate.instant(key, params);
  }

  private extractBackendMessage(error: unknown): string | undefined {
    const maybeHttpError = error as { error?: { message?: string; Message?: string } };
    return maybeHttpError?.error?.message || maybeHttpError?.error?.Message;
  }  
}
