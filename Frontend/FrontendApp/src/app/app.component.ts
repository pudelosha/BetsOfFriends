import { Component } from '@angular/core';
import { IonApp, IonRouterOutlet } from '@ionic/angular/standalone';
import { Router, NavigationEnd  } from '@angular/router';
import { AuthService } from './services/auth.service';
import { FormsModule } from '@angular/forms';
import { IonContent, IonHeader, IonTitle, IonToolbar, IonFooter, IonButton, IonButtons, IonMenuButton} from '@ionic/angular/standalone';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-root',
  templateUrl: 'app.component.html',
  imports: [IonApp, IonRouterOutlet, IonContent, IonHeader, IonTitle, IonToolbar, IonFooter, IonButton, IonButtons, IonMenuButton, CommonModule, FormsModule],
  standalone: true,
})
export class AppComponent {
  isLoggedIn = false;

  constructor(private authService: AuthService, private router: Router) {}

  ngOnInit() {
    // Subscribe to authentication changes
    this.authService.getAuthStatus().subscribe((loggedIn) => {
      this.isLoggedIn = loggedIn;
      console.log('Auth status changed:', loggedIn);
    });

    this.router.events.subscribe(event => {
      if (event instanceof NavigationEnd) {
        console.log('Router navigation completed. Current URL:', this.router.url);

        if (this.isLoggedIn && (this.router.url === '/' || this.router.url === '/welcome')) {
          console.log('Redirecting to /home');
          this.router.navigateByUrl('/home', { replaceUrl: true });
        }
      }
    });
  }

  logout() {
    this.authService.logout();
    this.isLoggedIn = false;
    this.router.navigate(['/welcome']);
  }
}
