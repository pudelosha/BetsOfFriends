import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { FormBuilder, FormGroup, Validators, AbstractControl, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ToastController, LoadingController } from '@ionic/angular';
import { AuthService } from '../../../services/auth.service';
import { ViewChild } from '@angular/core';
import { LanguageFabComponent } from '../../language/language-fab/language-fab.component';
import { TranslateModule } from '@ngx-translate/core';
import { IonContent, IonItem, IonLabel, IonInput, IonButton } from '@ionic/angular/standalone';

@Component({
  selector: 'app-login',
  templateUrl: './login.page.html',
  styleUrls: ['./login.page.scss'],
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TranslateModule, LanguageFabComponent, IonContent, IonItem, IonLabel, IonInput, IonButton]
})
export class LoginPage {
  @ViewChild(IonContent) content!: IonContent;

  parallaxOffset = 0;
  loginForm: FormGroup;
  errorMessage: string = '';
  isLoading: boolean = false;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router,
    private toastController: ToastController,
    private loadingController: LoadingController
  ) {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', Validators.required],
    });
  }

  get f(): { [key: string]: AbstractControl } {
    return this.loginForm.controls;
  }

  ionViewWillEnter() {
    this.scrollToTop();
    this.loginForm.reset();
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

  async login() {
    console.log('login() called');
    console.log('Form valid:', this.loginForm.valid);
    console.log('Form value:', this.loginForm.value);
  
    if (!this.loginForm.valid) return;
  
    const { email, password } = this.loginForm.value;
    console.log('Email and password extracted:', email, password);
  
    this.isLoading = true;
    const startTime = Date.now();
    let loading: HTMLIonLoadingElement | null = null;
  
    try {
      console.log('Creating loading spinner...');
      loading = await this.loadingController.create({
        message: 'Logging in...',
        spinner: 'crescent',
      });
  
      console.log('Presenting loading spinner...');
      await loading.present();
    } catch (err) {
      console.error('Error creating/presenting loading spinner:', err);
      this.isLoading = false;
      this.showToast('Unexpected UI error. Try again.', 'danger');
      return;
    }
  
    console.log('Calling authService.login()...');
    this.authService.login(email, password).subscribe({
      next: async (response) => {
        console.log('Login successful, response:', response);
        const elapsed = Date.now() - startTime;
        const delay = Math.max(0, 800 - elapsed);
  
        setTimeout(async () => {
          if (loading) await loading.dismiss();
          this.isLoading = false;
  
          if (response.success) {
            await this.showToast(response.message, 'success');
            this.router.navigate(['/home']);
          } else {
            this.showToast(response.message || 'Login failed.', 'danger');
          }
        }, delay);
      },
      error: async (error) => {
        console.error('Login error caught in component:', error);
        const elapsed = Date.now() - startTime;
        const delay = Math.max(0, 800 - elapsed);
  
        setTimeout(async () => {
          if (loading) await loading.dismiss();
          this.isLoading = false;
  
          const errorMsg = error?.error?.message || 'An error occurred during login.';
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

  // Navigation methods
  navigateToRegister() {
    this.router.navigateByUrl('/register');
  }

  navigateToForgotPassword() {
    this.router.navigateByUrl('/forgot-password');
  }

  navigateToResendActivation() {
    this.router.navigateByUrl('/resend-activation');
  }
}
