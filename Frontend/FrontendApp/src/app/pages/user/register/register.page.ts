import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { FormBuilder, FormGroup, Validators, AbstractControl, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ToastController, LoadingController } from '@ionic/angular';
import { RegisterService } from '../../../services/register.service';
import { ViewChild } from '@angular/core';
import { LanguageFabComponent } from '../../language/language-fab/language-fab.component';
import { TranslateModule } from '@ngx-translate/core';
import { IonContent, IonItem, IonLabel, IonInput, IonCheckbox, IonButton } from '@ionic/angular/standalone';

@Component({
  selector: 'app-register',
  templateUrl: './register.page.html',
  styleUrls: ['./register.page.scss'],
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TranslateModule, LanguageFabComponent, IonContent, IonItem, IonLabel, IonInput, IonCheckbox, IonButton]
})
export class RegisterPage {
  @ViewChild(IonContent) content!: IonContent;

  parallaxOffset = 0;
  registerForm: FormGroup;
  errorMessage: string = '';
  isLoading: boolean = false;

  constructor(
    private fb: FormBuilder,
    private registerService: RegisterService,
    private router: Router,
    private toastController: ToastController,
    private loadingController: LoadingController
  ) {
    this.registerForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8)]],
      confirmPassword: ['', Validators.required],
      consent: [false, Validators.requiredTrue],
    }, { validator: this.passwordsMatch });
  }


  ionViewWillEnter() {
    this.scrollToTop();
    this.registerForm.reset();
  }

  scrollToTop() {
    if (this.content) {
      this.content.scrollToTop(300);
    }
  }

  onScroll(event: any) {
    const scrollTop = event.detail.scrollTop;
    this.parallaxOffset = scrollTop * 0.4; // Adjust speed
  }

  toggleConsent(event: Event): void {
    // Prevent toggling if clicking the actual <a> link
    if ((event.target as HTMLElement).tagName.toLowerCase() === 'a') return;
  
    const control = this.registerForm.get('consent');
    if (control) {
      control.setValue(!control.value);
    }
  }
  
  openTerms(event: Event): void {
    event.stopPropagation(); // prevent it from bubbling to ion-label
    this.router.navigate(['/terms']);
  }
  
  get f(): { [key: string]: AbstractControl } {
    return this.registerForm.controls;
  }

  passwordsMatch(group: FormGroup): { [key: string]: boolean } | null {
    const password = group.get('password')?.value;
    const confirmPassword = group.get('confirmPassword')?.value;
    return password === confirmPassword ? null : { passwordsMismatch: true };
  }

  async register() {
    if (!this.registerForm.valid) return;
  
    const { email, password, consent } = this.registerForm.value;
    const language = localStorage.getItem('lang') || 'en';
  
    // Show loading spinner
    this.isLoading = true;
    const loading = await this.loadingController.create({
      message: 'Creating your account...',
      spinner: 'crescent',
    });
    await loading.present();
  
    const startTime = Date.now();
  
    this.registerService.register({ email, password, consent, language }).subscribe({
      next: async (response) => {
        const elapsed = Date.now() - startTime;
        const delay = Math.max(0, 800 - elapsed);
  
        setTimeout(async () => {
          await loading.dismiss();
          this.isLoading = false;
  
          if (response.success) {
            this.showToast(response.message, 'success');
            this.router.navigate(['/login']);
          } else {
            this.showToast(response.message || 'Registration failed.', 'danger');
          }
        }, delay);
      },
      error: async (error) => {
        const elapsed = Date.now() - startTime;
        const delay = Math.max(0, 800 - elapsed);
  
        setTimeout(async () => {
          await loading.dismiss();
          this.isLoading = false;
  
          console.error('Registration error:', error);
          const errorMsg = error?.error?.message || 'An error occurred. Please try again.';
          this.showToast(errorMsg, 'danger');
        }, delay);
      }
    });
  }   

  async showToast(message: string, color: string) {
    const toast = await this.toastController.create({
      message,
      duration: 3000,
      position: 'bottom',
      color,
    });
    await toast.present();
  }

  navigateToLogin() {
    this.router.navigateByUrl('/login');
  }
}
