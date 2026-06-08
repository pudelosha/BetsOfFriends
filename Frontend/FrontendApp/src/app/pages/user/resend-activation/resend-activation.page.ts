import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators, AbstractControl, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ToastController, LoadingController } from '@ionic/angular';
import { RegisterService } from 'src/app/services/register.service';
import { Router } from '@angular/router';
import { ViewChild } from '@angular/core';
import { LanguageFabComponent } from '../../language/language-fab/language-fab.component';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { IonContent, IonItem, IonLabel, IonInput, IonButton } from '@ionic/angular/standalone';
import { BackendMessageService } from 'src/app/services/backend-message.service';

@Component({
  selector: 'app-resend-activation',
  templateUrl: './resend-activation.page.html',
  styleUrls: ['./resend-activation.page.scss'],
  imports: [CommonModule, ReactiveFormsModule, TranslateModule, LanguageFabComponent, IonContent, IonItem, IonLabel, IonInput, IonButton],
  standalone: true,
})
export class ResendActivationPage {
  @ViewChild(IonContent) content!: IonContent;

  parallaxOffset = 0;
  resendActivationForm: FormGroup;
  isLoading: boolean = false;

  constructor(
    private fb: FormBuilder,
    private registerService: RegisterService,
    private backendMessages: BackendMessageService,
    private translate: TranslateService,
    private toastController: ToastController,
    private router: Router,
    private loadingController: LoadingController
  ) {
    this.resendActivationForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
    });
  }

  get f(): { [key: string]: AbstractControl } {
    return this.resendActivationForm.controls;
  }

  ionViewWillEnter() {
    this.scrollToTop();
    this.resendActivationForm.reset();
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

  async showToast(message: string, color: 'success' | 'danger') {
    const toast = await this.toastController.create({
      message,
      duration: 3000,
      position: 'bottom',
      color,
    });
    await toast.present();
  }

  async resendActivation() {
    if (!this.resendActivationForm.valid) return;
  
    const { email } = this.resendActivationForm.value;
  
    this.isLoading = true;
  
    const loading = await this.loadingController.create({
      message: this.translate.instant('AUTH_STATUS.RESEND_LOADING'),
      spinner: 'crescent',
    });
    await loading.present();
  
    const startTime = Date.now();
  
    this.registerService.resendActivationEmail(email).subscribe({
      next: async (response) => {
        const elapsed = Date.now() - startTime;
        const delay = Math.max(0, 800 - elapsed);
  
        setTimeout(async () => {
          await loading.dismiss();
          this.isLoading = false;
  
          const message = this.backendMessages.translateMessage(
            response.message,
            response.success ? 'AUTH_STATUS.CONFIRMATION_EMAIL_SENT' : 'AUTH_STATUS.UNEXPECTED_ERROR',
            response.success
          );
          this.showToast(message, response.success ? 'success' : 'danger');
        }, delay);
      },
      error: async (error) => {
        const elapsed = Date.now() - startTime;
        const delay = Math.max(0, 800 - elapsed);
  
        setTimeout(async () => {
          await loading.dismiss();
          this.isLoading = false;
  
          this.showToast(this.translate.instant('AUTH_STATUS.UNEXPECTED_ERROR'), 'danger');
          console.error('Resend Activation Error:', error);
        }, delay);
      }
    });
  }
  
  navigateToLogin() {
    this.router.navigate(['/login']);
  }
}
