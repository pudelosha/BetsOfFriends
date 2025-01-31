import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators, AbstractControl, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { IonicModule, ToastController } from '@ionic/angular';
import { RegisterService } from 'src/app/services/register.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-resend-activation',
  templateUrl: './resend-activation.page.html',
  styleUrls: ['./resend-activation.page.scss'],
  imports: [CommonModule, IonicModule, ReactiveFormsModule],
  standalone: true,
})
export class ResendActivationPage {
  resendActivationForm: FormGroup;

  constructor(
    private fb: FormBuilder,
    private registerService: RegisterService,
    private toastController: ToastController,
    private router: Router
  ) {
    this.resendActivationForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
    });
  }

  get f(): { [key: string]: AbstractControl } {
    return this.resendActivationForm.controls;
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

  resendActivation() {
    if (this.resendActivationForm.valid) {
      const { email } = this.resendActivationForm.value;

      this.registerService.resendActivationEmail(email).subscribe({
        next: (response) => {
          this.showToast(response.message, response.success ? 'success' : 'danger');
        },
        error: (error) => {
          this.showToast("An unexpected error occurred.", 'danger');
          console.error("Resend Activation Error:", error);
        },
      });
    }
  }

  navigateToWelcome() {
    this.router.navigate(['/welcome']);
  }
}
