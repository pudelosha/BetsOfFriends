import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormGroup, FormBuilder, Validators, AbstractControl } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { SupportService } from 'src/app/services/support.service';
import { IonicModule, ToastController } from '@ionic/angular';
import { TitleService } from 'src/app/services/title.service';
import { LanguageService } from 'src/app/services/language.service';
import { firstValueFrom } from 'rxjs';
import { AuthService } from 'src/app/services/auth.service';

@Component({
  selector: 'app-support',
  templateUrl: './support.page.html',
  styleUrls: ['./support.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule, FormsModule, TranslateModule],
})
export class SupportPage implements OnInit {
  supportForm!: FormGroup;
  isLoading = false;
  isLoggedIn = false;

  constructor(
    private fb: FormBuilder,
    private supportService: SupportService,
    private toastController: ToastController,
    private titleService: TitleService,
    private languageService: LanguageService,
    private authService: AuthService
  ) {}

  ngOnInit() {
    this.isLoggedIn = this.authService.isLoggedIn();
    const userEmail = this.isLoggedIn ? this.authService.getEmailFromToken() : '';

    this.supportForm = this.fb.group({
      email: [{ value: userEmail, disabled: this.isLoggedIn }, [Validators.required, Validators.email]],
      subject: ['', Validators.required],
      message: ['', Validators.required]
    });

    this.titleService.setTitle('SUPPORT.TITLE');
  }

  ionViewWillEnter() {
    this.titleService.setTitle('SUPPORT.TITLE');
  }

  get f(): { [key: string]: AbstractControl } {
    return this.supportForm.controls;
  }

  async submitSupportRequest() {
    if (this.supportForm.invalid) return;

    this.isLoading = true;

    const rawFormValue = this.supportForm.getRawValue(); // includes disabled controls
    const { email, subject, message } = rawFormValue;
    const language = this.languageService.currentLang || 'en';

    try {
      await firstValueFrom(this.supportService.sendSupportMessage({ email, subject, message, language }));
      this.supportForm.reset();

      if (this.isLoggedIn) {
        this.supportForm.controls['email'].setValue(email); // restore locked email after reset
        this.supportForm.controls['email'].disable();
      }

      this.showToast('Message sent successfully', 'success');
    } catch (err) {
      this.showToast('Failed to send message. Try again later.', 'danger');
    } finally {
      this.isLoading = false;
    }
  }

  async showToast(msg: string, color: 'success' | 'danger') {
    const toast = await this.toastController.create({
      message: msg,
      duration: 3000,
      color,
      position: 'bottom'
    });
    await toast.present();
  }
}
