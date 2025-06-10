import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators, AbstractControl, ReactiveFormsModule } from '@angular/forms';
import { UserService } from '..//..//../services//user.service';
import { ToastController, LoadingController } from '@ionic/angular';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ViewChild } from '@angular/core';
import { LanguageFabComponent } from '../../language/language-fab/language-fab.component';
import { TranslateModule } from '@ngx-translate/core';
import { IonContent, IonItem, IonLabel, IonInput, IonButton } from '@ionic/angular/standalone';

@Component({
  selector: 'app-forgot-password',
  templateUrl: './forgot-password.page.html',
  styleUrls: ['./forgot-password.page.scss'],
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, LanguageFabComponent, TranslateModule, IonContent, IonItem, IonLabel, IonInput, IonButton]
})
export class ForgotPasswordPage {
  @ViewChild(IonContent) content!: IonContent;

  parallaxOffset = 0;
  forgotPasswordForm: FormGroup;
  isLoading = false;

  constructor(
    private fb: FormBuilder,
    private userService: UserService,
    private toastController: ToastController,
    private router: Router,
    private loadingController: LoadingController
  ) {
    this.forgotPasswordForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]]
    });
  }

  get f(): { [key: string]: AbstractControl } {
    return this.forgotPasswordForm.controls;
  }

  ionViewWillEnter() {
    this.scrollToTop();
    this.forgotPasswordForm.reset();
  }

  scrollToTop() {
    if (this.content) {
      this.content.scrollToTop(300);
    }
  }

  onScroll(event: any) {
    const scrollTop = event.detail.scrollTop;
    this.parallaxOffset = scrollTop * 0.4;
  }

  async submitRequest() {
    if (this.forgotPasswordForm.invalid) return;
  
    this.isLoading = true;
    const email = this.forgotPasswordForm.value.email;
  
    const loading = await this.loadingController.create({
      message: 'Sending reset link...',
      spinner: 'crescent',
    });
    await loading.present();
  
    const startTime = Date.now();
  
    this.userService.forgotPassword(email).subscribe({
      next: async (response) => {
        const elapsed = Date.now() - startTime;
        const delay = Math.max(0, 800 - elapsed);
  
        setTimeout(async () => {
          await loading.dismiss();
          this.isLoading = false;
  
          this.showToast(response.message || 'Password reset link sent!', 'success');
        }, delay);
      },
      error: async () => {
        const elapsed = Date.now() - startTime;
        const delay = Math.max(0, 800 - elapsed);
  
        setTimeout(async () => {
          await loading.dismiss();
          this.isLoading = false;
  
          this.showToast('Something went wrong. Please try again.', 'danger');
        }, delay);
      },
    });
  }
  
  async showToast(message: string, color: 'success' | 'danger') {
    const toast = await this.toastController.create({
      message,
      duration: 3000,
      position: 'bottom',
      color
    });
    await toast.present();
  }

  navigateToLogin() {
    this.router.navigate(['/login']);
  }
}
