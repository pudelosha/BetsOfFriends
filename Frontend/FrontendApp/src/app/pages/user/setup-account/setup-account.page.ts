import { Component } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, FormGroup, Validators, AbstractControl, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ToastController } from '@ionic/angular';
import { RegisterService } from '../../../services/register.service';
import { LanguageFabComponent } from '../../language/language-fab/language-fab.component';
import { TranslateModule } from '@ngx-translate/core';
import { IonContent, IonItem, IonLabel, IonInput, IonButton, IonCheckbox } from '@ionic/angular/standalone';

@Component({
  selector: 'app-setup-account',
  templateUrl: './setup-account.page.html',
  styleUrls: ['./setup-account.page.scss'],
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TranslateModule, LanguageFabComponent, IonContent, IonItem, IonLabel, IonInput, IonButton, IonCheckbox]
})
export class SetupAccountPage {
  setupForm: FormGroup;
  isLoading = true;
  userId: string = '';
  token: string = '';
  message = '';
  parallaxOffset = 0;

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

  onScroll(event: any) {
    const scrollTop = event.detail.scrollTop;
    this.parallaxOffset = scrollTop * 0.4; // Adjust speed
  }

  toggleConsent(event: Event): void {
    if ((event.target as HTMLElement).tagName.toLowerCase() === 'a') return;
    const control = this.setupForm.get('consent');
    if (control) {
      control.setValue(!control.value);
    }
  }
  
  openTerms(event: Event): void {
    event.stopPropagation();
    this.router.navigate(['/terms']);
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
      const language = localStorage.getItem('lang') || 'en';
      
      this.registerService.setupAccount(this.userId, this.token, password, language).subscribe({
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
