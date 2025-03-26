import { Component } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { IonicModule, ToastController } from '@ionic/angular';
import { CommonModule } from '@angular/common';
import { RegisterService } from 'src/app/services/register.service';
import { ViewChild } from '@angular/core';
import { IonContent } from '@ionic/angular';

@Component({
  selector: 'app-confirm-email',
  templateUrl: './confirm-email.page.html',
  styleUrls: ['./confirm-email.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule],
})
export class ConfirmEmailPage {
  @ViewChild(IonContent) content!: IonContent;

  isLoading = true;
  confirmationSuccess = false;
  message = '';
  parallaxOffset = 0;

  constructor(
    private route: ActivatedRoute,
    private registerService: RegisterService,
    private toastController: ToastController,
    private router: Router
  ) {}

  ngOnInit() {
    this.route.queryParams.subscribe(params => {
      const userId = params['userId'];
      const token = params['token'];

      if (!userId || !token) {
        this.message = "Invalid confirmation link.";
        this.isLoading = false;
        return;
      }

      this.registerService.confirmEmail(userId, token).subscribe({
        next: (response) => {
          this.confirmationSuccess = response.success;
          this.message = response.message || "Your email has been confirmed.";
          this.isLoading = false;
          this.showToast(this.message, "success");
        },
        error: () => {
          this.confirmationSuccess = false;
          this.message = "An error occurred while confirming your email.";
          this.isLoading = false;
          this.showToast(this.message, "danger");
        }
      });
    });
  }

  ionViewWillEnter() {
    this.scrollToTop();
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

  async showToast(message: string, color: 'success' | 'danger') {
    const toast = await this.toastController.create({
      message,
      duration: 3000,
      position: 'bottom',
      color,
    });
    await toast.present();
  }

  navigateToLogin() {
    this.router.navigate(['/login']);
  }
}
