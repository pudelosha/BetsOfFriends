import { Component  } from '@angular/core';
import { IonApp, IonRouterOutlet, IonItemDivider, IonFab, IonFabButton } from '@ionic/angular/standalone';
import { Router, NavigationEnd  } from '@angular/router';
import { AuthService } from './services/auth.service';
import { FormsModule } from '@angular/forms';
import { IonContent, IonHeader, IonTitle, IonToolbar, IonFooter, IonButtons, IonMenuButton, IonMenu, IonList, IonItem, IonIcon, IonLabel, IonMenuToggle } from '@ionic/angular/standalone';
import { CommonModule } from '@angular/common';
import { filter } from 'rxjs/operators';
import { ToastController, MenuController } from '@ionic/angular';

@Component({
  selector: 'app-root',
  templateUrl: 'app.component.html',
  imports: [IonFabButton, IonFab, IonItemDivider, IonApp, IonRouterOutlet, IonContent, IonHeader, IonTitle, IonToolbar, IonFooter, IonButtons, IonMenuButton, IonMenu, IonList, IonMenuToggle, IonItem, IonIcon, IonLabel, IonFabButton, CommonModule, FormsModule],
  standalone: true,
})
export class AppComponent {
  isLoggedIn = false;
  isSuperAdmin = false;
  isAdmin = false;
  showFab: boolean = false; // Control FAB visibility

  constructor(private authService: AuthService, private router: Router, private toastController: ToastController, private menuCtrl: MenuController) {}

  ngOnInit() {
    // Subscribe to authentication status changes
    this.authService.getAuthStatus().subscribe((loggedIn) => {
      this.isLoggedIn = loggedIn;
      console.log('Auth status changed:', loggedIn);
      this.updateUserRoles();
    });

    // Monitor navigation changes
    this.router.events.pipe(filter(event => event instanceof NavigationEnd)).subscribe((event: NavigationEnd) => {
      console.log('Navigation ended, checking auth state and FAB visibility');

      // Refresh authentication state
      this.isLoggedIn = this.authService.isLoggedIn();
      this.updateUserRoles();

      // Show FAB only on `/my-bets/to-place`
      this.showFab = event.urlAfterRedirects === '/my-bets/to-place';
      console.log("FAB Visibility:", this.showFab);
    });
  }

  updateUserRoles() {
    const userRoles = this.authService.getUserRoles();
    console.log("User roles:", userRoles);

    this.isSuperAdmin = userRoles.includes("SuperAdmin");
    this.isAdmin = this.isSuperAdmin || userRoles.includes("Admin"); // Admin or SuperAdmin
  }

  openFilters() {
    console.log("Filter button clicked - Implement filter modal here!");
  }

  logout() {
    console.log('Logging out...');
    this.authService.logout();
    this.isLoggedIn = false;
  
    console.log('Navigating to logoff page...');
    this.router.navigate(['/logoff']);
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

  navigateToProfile() {
    console.log('Navigating to profile page...');
    this.router.navigate(['/profile']);
  }

  navigateToNotificationSettings() {
    console.log('Navigating to notification-settings...');
    this.router.navigate(['/notification-settings']);
  }

  navigateToMyBets() {
    console.log('Navigating to my-bets...');
    this.router.navigate(['/my-bets']);
  }

  navigateToBetsOverview() {
    console.log('Navigating to bets-overview...');
    this.router.navigate(['/bets-overview']);
  }

  navigateToCreatePredefinedTournament() {
    console.log('Navigating to create-predefined-tournament...');
    this.router.navigate(['/tournaments/create-predefined']);
  }

  navigateToPredefinedTournamentsList() {
    console.log('Navigating to predefined-tournaments-list...');
    this.router.navigate(['/tournaments/predefined']);
  }

  navigateToMyTournaments() {
    console.log('Navigating to my-tournaments...');
    this.router.navigate(['/my-tournaments']);
  }

  navigateToCreateCustomTournament() {
    console.log('Navigating to create-custom-tournament...');
    this.router.navigate(['/tournaments/create-custom']);
  }

  navigateToCustomTournamentsList() {
    console.log('Navigating to custom-tournament-list...');
    this.router.navigate(['/tournaments/custom']);
  }

  navigateToCustomTournamentsMatches() {
    console.log('Navigating to custom-tournament-matches...');
    this.router.navigate(['/matches']);
  }

  navigateToHome() {
    console.log('Navigating to home...');
    this.router.navigate(['/home']);
  }

  navigateToTournamentSummary(){
    console.log('Navigating to summary...');
    this.router.navigate(['/summary']);
  }

  navigateToTournamentLiveResults() {
    console.log('Navigating to live-results...');
    this.router.navigate(['/live-results']);
  }

}
