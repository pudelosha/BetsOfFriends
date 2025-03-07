import { Component } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, FormGroup, Validators, AbstractControl, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { IonicModule, ToastController } from '@ionic/angular';
import { RegisterService } from '../../../services/register.service';

@Component({
  selector: 'app-setup-account',
  templateUrl: './setup-account.page.html',
  styleUrls: ['./setup-account.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule],
})
export class SetupAccountPage {
  setupForm: FormGroup;
  isLoading = true;
  userId: string = '';
  token: string = '';
  message = '';

  constructor(
    private route: ActivatedRoute,
    private fb: FormBuilder,
    private registerService: RegisterService,
    private router: Router,
    private toastController: ToastController
  ) {
    this.setupForm = this.fb.group(
      {
        password: ['', [Validators.required, Validators.minLength(8)]],
        confirmPassword: ['', Validators.required],
        consent: [false, Validators.requiredTrue],
      },
      { validator: this.passwordsMatch }
    );
  }

  get f(): { [key: string]: AbstractControl } {
    return this.setupForm.controls;
  }

  passwordsMatch(group: FormGroup): { [key: string]: boolean } | null {
    const password = group.get('password')?.value;
    const confirmPassword = group.get('confirmPassword')?.value;
    return password === confirmPassword ? null : { passwordsMismatch: true };
  }

  ngOnInit() {
    this.route.queryParams.subscribe(params => {
      this.userId = params['userId'];
      this.token = params['token'];
      
      if (!this.userId || !this.token) {
        this.message = 'Invalid or expired setup link.';
        this.isLoading = false;
      } else {
        this.isLoading = false;
      }
    });
  }

  async submit() {
    if (this.setupForm.valid) {
      const { password, consent } = this.setupForm.value;
      
      this.registerService.setupAccount(this.userId, this.token, password).subscribe({
        next: async (response) => {
          if (response.success) {
            this.showToast('Your account has been set up successfully!', 'success');
            this.router.navigate(['/login']);
          } else {
            this.showToast(response.message || 'Setup failed.', 'danger');
          }
        },
        error: () => {
          this.showToast('An error occurred. Please try again.', 'danger');
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
}
