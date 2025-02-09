import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, FormGroup, Validators, AbstractControl, ReactiveFormsModule } from '@angular/forms';
import { UserService } from '../../../services/user.service';
import { IonicModule, ToastController } from '@ionic/angular';
import { ViewChild } from '@angular/core';
import { IonContent } from '@ionic/angular';

@Component({
  selector: 'app-reset-password',
  templateUrl: './reset-password.page.html',
  styleUrls: ['./reset-password.page.scss'],
  imports: [CommonModule, IonicModule, ReactiveFormsModule],
  standalone: true,
})
export class ResetPasswordPage implements OnInit {
  @ViewChild(IonContent) content!: IonContent;

  ionViewWillEnter() {
    this.scrollToTop();
    this.resetPasswordForm.reset();
  }

  scrollToTop() {
    if (this.content) {
      this.content.scrollToTop(300);
    }
  }

  resetPasswordForm: FormGroup;
  userId: string = '';
  token: string = '';

  constructor(
    private fb: FormBuilder,
    private userService: UserService,
    private route: ActivatedRoute,
    private router: Router,
    private toastController: ToastController
  ) {
    this.resetPasswordForm = this.fb.group({
      password: ['', [Validators.required, Validators.minLength(8)]],
      confirmPassword: ['', [Validators.required]]
    }, { validator: this.passwordsMatchValidator });
  }

  ngOnInit() {
    this.userId = this.route.snapshot.queryParamMap.get('userId') || '';
    this.token = this.route.snapshot.queryParamMap.get('token') || '';
  }

  get f(): { [key: string]: AbstractControl } {
    return this.resetPasswordForm.controls;
  }

  async submitNewPassword() {
    if (this.resetPasswordForm.invalid) return;

    const { password, confirmPassword } = this.resetPasswordForm.value;

    this.userService.resetPassword(this.userId, this.token, password).subscribe({
      next: async (response) => {
        const toast = await this.toastController.create({
          message: response.message,
          duration: 3000,
          position: 'bottom',
          color: response.success ? 'success' : 'danger'
        });
        await toast.present();

        if (response.success) {
          this.router.navigate(['/login']);
        }
      },
      error: async () => {
        const toast = await this.toastController.create({
          message: 'An error occurred. Please try again.',
          duration: 3000,
          position: 'bottom',
          color: 'danger'
        });
        await toast.present();
      }
    });
  }

  passwordsMatchValidator(group: FormGroup) {
    return group.get('password')!.value === group.get('confirmPassword')!.value
      ? null
      : { notMatching: true };
  }

  navigateToLogin() {
    this.router.navigate(['/login']);
  }
}
