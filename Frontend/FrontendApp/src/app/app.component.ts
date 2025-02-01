import { Component  } from '@angular/core';
import { IonApp, IonRouterOutlet, IonItemDivider } from '@ionic/angular/standalone';
import { Router, NavigationEnd  } from '@angular/router';
import { AuthService } from './services/auth.service';
import { FormsModule } from '@angular/forms';
import { IonContent, IonHeader, IonTitle, IonToolbar, IonFooter, IonButtons, IonMenuButton, IonMenu, IonList, IonItem, IonIcon, IonLabel } from '@ionic/angular/standalone';
import { CommonModule } from '@angular/common';
import { filter } from 'rxjs/operators';
import { ToastController } from '@ionic/angular';

@Component({
  selector: 'app-root',
  templateUrl: 'app.component.html',
  imports: [IonItemDivider, IonApp, IonRouterOutlet, IonContent, IonHeader, IonTitle, IonToolbar, IonFooter, IonButtons, IonMenuButton, IonMenu, IonList, IonItem, IonIcon, IonLabel, CommonModule, FormsModule],
  standalone: true,
})
export class AppComponent {
  isLoggedIn = false;

  constructor(private authService: AuthService, private router: Router, private toastController: ToastController) {}

  ngOnInit() {
    // Subscribe to authentication changes
    this.authService.getAuthStatus().subscribe((loggedIn) => {
      this.isLoggedIn = loggedIn;
      console.log('Auth status changed:', loggedIn);
    });

    // Force UI refresh on navigation
    this.router.events
      .pipe(filter(event => event instanceof NavigationEnd))
      .subscribe(() => {
        console.log('Navigation ended, checking auth state again');
        this.isLoggedIn = this.authService.isLoggedIn();
      });
  }

  async logout() {
    console.log('Logging out...');
    this.authService.logout();
    this.isLoggedIn = false;
  
    // Show logout toast for 3 seconds
    await this.presentToast('You have been logged out.', 'success');
  
    console.log('Waiting 3 seconds before redirecting...');
    
    // Delay navigation for 3 seconds
    setTimeout(() => {
      console.log('Navigating to login page...');
      this.router.navigate(['/login']).then(() => {
        window.location.reload();
      });
    }, 3000);
  }

  async presentToast(message: string, color: string) {
    const toast = await this.toastController.create({
      message,
      duration: 3000, // Show for 3 seconds
      position: 'bottom',
      color, // success, warning, danger, etc.
    });
    await toast.present();
  }
}
