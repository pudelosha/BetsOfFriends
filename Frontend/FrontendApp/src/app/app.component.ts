import { Component  } from '@angular/core';
import { IonicModule } from '@ionic/angular';
import { Router, NavigationEnd  } from '@angular/router';
import { AuthService } from './services/auth.service';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { filter } from 'rxjs/operators';
import { ToastController, MenuController, ModalController } from '@ionic/angular';
import { ParticipantTournamentsModalComponent } from './modals/participant-tournaments-modal/participant-tournaments-modal.component';
import { TranslateModule } from '@ngx-translate/core';
import { LanguageFabComponent } from './pages/language/language-fab/language-fab.component';


@Component({
  selector: 'app-root',
  templateUrl: 'app.component.html',
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule, TranslateModule, LanguageFabComponent],
})
export class AppComponent {
  isLoggedIn = false;
  isSuperAdmin = false;
  isAdmin = false;
  showFab: boolean = false; // Control FAB visibility

  constructor(private authService: AuthService, private router: Router, private toastController: ToastController, private menuCtrl: MenuController, private modalController: ModalController) {}

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

  async openFootballModal() {
    const modal = await this.modalController.create({
      component: ParticipantTournamentsModalComponent,
      breakpoints: [0, 0.5, 0.8],
      initialBreakpoint: 1,
      backdropDismiss: true
    });
    return await modal.present();
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

  navigateToMessages(){
    this.router.navigate(['/messages']);
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

  navigateToCreatePredefinedTournament() {
    console.log('Navigating to create-predefined-tournament...');
    this.router.navigate(['/tournaments/create-predefined']);
  }

  navigateToPredefinedTournamentsList() {
    console.log('Navigating to predefined-tournaments-list...');
    this.router.navigate(['/tournaments/predefined']);
  }

  navigateToPredefinedTournamentsMatches() {
    console.log('Navigating to predefined-tournament-matches...');
    this.router.navigate(['/matches/predefined']);
  }

  navigateToManageUsers(){
    console.log('Navigating to manage-users...');
    this.router.navigate(['/users']);
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
    this.router.navigate(['/matches/custom']);
  }

  navigateToCustomTournamentsParticipants() {
    console.log('Navigating to custom-tournament-participants...');
    this.router.navigate(['/tournaments/participants']);
  }

  navigateToHome() {
    console.log('Navigating to home...');
    this.router.navigate(['/home']);
  }

  navigateToTournamentSummary(){
    console.log('Navigating to summary...');
    this.router.navigate(['/summary']);
  }

  navigateToFindTournament() {
    this.router.navigate(['/find-tournament']);
  }  
}
