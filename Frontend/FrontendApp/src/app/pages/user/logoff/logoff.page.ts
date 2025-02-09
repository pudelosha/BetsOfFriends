import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { IonicModule } from '@ionic/angular';
import { ReactiveFormsModule } from '@angular/forms';
import { ToastController } from '@ionic/angular';

@Component({
  selector: 'app-logoff',
  templateUrl: './logoff.page.html',
  styleUrls: ['./logoff.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule],
})
export class LogoffPage {
  constructor(private router: Router, private toastController: ToastController) {}

  async ionViewWillEnter() {
    console.log('Logoff page entered. Showing toast message...');
    
    // Show toast message
    await this.presentToast('You have been logged out.', 'success');

    console.log('Waiting 3 seconds before redirecting...');
    
    // Wait 3 seconds before redirecting
    setTimeout(() => {
      console.log('Navigating to login page...');
      this.router.navigate(['/login']);
    }, 3000);
  }

  async presentToast(message: string, color: string) {
    const toast = await this.toastController.create({
      message,
      duration: 3000, // Toast lasts for 3 seconds
      color,
      position: 'bottom',
    });
    await toast.present();
  }

  navigateToLogin() {
    this.router.navigateByUrl('/login');
  }
}