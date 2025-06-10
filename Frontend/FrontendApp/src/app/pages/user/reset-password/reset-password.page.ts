import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, FormGroup, Validators, AbstractControl, ReactiveFormsModule } from '@angular/forms';
import { UserService } from '../../../services/user.service';
import { ToastController, LoadingController } from '@ionic/angular';
import { ViewChild } from '@angular/core';
import { LanguageFabComponent } from '../../language/language-fab/language-fab.component';
import { TranslateModule } from '@ngx-translate/core';
import { IonContent, IonItem, IonLabel, IonInput, IonButton } from '@ionic/angular/standalone';

@Component({
  selector: 'app-reset-password',
  templateUrl: './reset-password.page.html',
  styleUrls: ['./reset-password.page.scss'],
  imports: [CommonModule, ReactiveFormsModule, TranslateModule, LanguageFabComponent, IonContent, IonItem, IonLabel, IonInput, IonButton],
  standalone: true,
})
export class ResetPasswordPage implements OnInit {
  @ViewChild(IonContent) content!: IonContent;

  parallaxOffset = 0;
  resetPasswordForm: FormGroup;
  userId: string = '';
  token: string = '';
  isLoading: boolean = false;

  constructor(
    private fb: FormBuilder,
    private userService: UserService,
    private route: ActivatedRoute,
    private router: Router,
    private toastController: ToastController,
    private loadingController: LoadingController
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

  ionViewWillEnter() {
    this.scrollToTop();
    this.resetPasswordForm.reset();
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

  async submitNewPassword() {
    if (this.resetPasswordForm.invalid) return;
  
    const { password, confirmPassword } = this.resetPasswordForm.value;
  
    this.isLoading = true;
  
    const loading = await this.loadingController.create({
      message: 'Resetting password...',
      spinner: 'crescent',
    });
    await loading.present();
  
    const startTime = Date.now();
  
    this.userService.resetPassword(this.userId, this.token, password).subscribe({
      next: async (response) => {
        const elapsed = Date.now() - startTime;
        const delay = Math.max(0, 800 - elapsed);
  
        setTimeout(async () => {
          await loading.dismiss();
          this.isLoading = false;
  
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
        }, delay);
      },
      error: async () => {
        const elapsed = Date.now() - startTime;
        const delay = Math.max(0, 800 - elapsed);
  
        setTimeout(async () => {
          await loading.dismiss();
          this.isLoading = false;
  
          const toast = await this.toastController.create({
            message: 'An error occurred. Please try again.',
            duration: 3000,
            position: 'bottom',
            color: 'danger'
          });
          await toast.present();
        }, delay);
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