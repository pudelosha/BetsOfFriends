import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { ToastController } from '@ionic/angular';
import { TranslateModule } from '@ngx-translate/core';
import { IonContent, IonButton } from '@ionic/angular/standalone';

@Component({
  selector: 'app-logoff',
  templateUrl: './logoff.page.html',
  styleUrls: ['./logoff.page.scss'],
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TranslateModule, IonContent, IonButton]
})
export class LogoffPage {
  parallaxOffset = 0;

  constructor(private router: Router, private toastController: ToastController) {}

  async ionViewWillEnter() {
    await this.presentToast('You have been logged out.', 'success');
    
    setTimeout(() => {
      this.router.navigate(['/welcome']);
    }, 3000);
  }

  onScroll(event: any) {
    const scrollTop = event.detail.scrollTop;
    this.parallaxOffset = scrollTop * 0.4;
  }

  async presentToast(message: string, color: string) {
    const toast = await this.toastController.create({
      message,
      duration: 3000,
      color,
      position: 'bottom',
    });
    await toast.present();
  }

  navigateToLogin() {
    this.router.navigateByUrl('/welcome');
  }
}