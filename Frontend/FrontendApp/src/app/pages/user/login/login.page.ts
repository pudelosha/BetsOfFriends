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
    if (!this.loginForm.valid) return;
  
    const { email, password } = this.loginForm.value;
  
    // Show loading spinner
    this.isLoading = true;
    const loading = await this.loadingController.create({
      message: 'Logging in...',
      spinner: 'crescent',
    });
    await loading.present();
  
    const startTime = Date.now();
  
    this.authService.login(email, password).subscribe({
      next: async (response) => {
        const elapsed = Date.now() - startTime;
        const delay = Math.max(0, 800 - elapsed);
  
        setTimeout(async () => {
          await loading.dismiss();
          this.isLoading = false;
  
          //console.log('Login response:', response);
  
          if (response.success) {
            this.showToast(response.message, 'success');
            this.router.navigate(['/home']);
          } else {
            this.showToast(response.message || 'Login failed.', 'danger');
          }
        }, delay);
      },
      error: async (error) => {
        const elapsed = Date.now() - startTime;
        const delay = Math.max(0, 800 - elapsed);
  
        setTimeout(async () => {
          await loading.dismiss();
          this.isLoading = false;
  
          console.error('Login error:', error);
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
