import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { FormBuilder, FormGroup, Validators, AbstractControl, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { IonicModule, ToastController } from '@ionic/angular';
import { RegisterService } from '../../services/register.service';
import { ViewChild } from '@angular/core';
import { IonContent } from '@ionic/angular';

@Component({
  selector: 'app-register',
  templateUrl: './register.page.html',
  styleUrls: ['./register.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule],
})
export class RegisterPage {
  @ViewChild(IonContent) content!: IonContent;

  ionViewWillEnter() {
    this.scrollToTop();
    this.registerForm.reset();
  }

  scrollToTop() {
    if (this.content) {
      this.content.scrollToTop(300);
    }
  }

  registerForm: FormGroup;
  errorMessage: string = '';

  constructor(
    private fb: FormBuilder,
    private registerService: RegisterService,
    private router: Router,
    private toastController: ToastController
  ) {
    this.registerForm = this.fb.group({
      userName: ['', [Validators.required, Validators.maxLength(50)]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8)]],
      confirmPassword: ['', Validators.required],
      consent: [false, Validators.requiredTrue],
    }, { validator: this.passwordsMatch });
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
    if (this.registerForm.valid) {
      const { userName, email, password, consent } = this.registerForm.value;
  
      this.registerService.register({ userName, email, password, consent }).subscribe({
        next: async (response) => {
          if (response.success) {
            this.showToast(response.message, 'success');
            this.router.navigate(['/login']);
          } else {
            this.showToast(response.message || 'Registration failed.', 'danger');
          }
        },
        error: (error) => {
          console.error('Registration error:', error);
          const errorMsg = error?.error?.message || 'An error occurred. Please try again.';
          this.showToast(errorMsg, 'danger');
        }
      });
    }
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
