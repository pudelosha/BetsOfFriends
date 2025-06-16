import { Component  } from '@angular/core';
import { Router, NavigationEnd  } from '@angular/router';
import { AuthService } from './services/auth.service';
import { ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { filter } from 'rxjs/operators';
import { ToastController, ModalController } from '@ionic/angular';
import { ParticipantTournamentsModalComponent } from './modals/participant-tournaments-modal/participant-tournaments-modal.component';
import { TranslateModule } from '@ngx-translate/core';
import { TitleService } from './services/title.service';
import { Platform } from '@ionic/angular';
import { IonMenu, IonApp, IonHeader, IonToolbar, IonTitle, IonContent, IonList, IonMenuToggle, IonItem, IonIcon, IonLabel, IonItemDivider, IonButtons, IonMenuButton, IonButton, IonFooter, IonRouterOutlet} from '@ionic/angular/standalone';
import { version } from 'src/environments/version';

@Component({
  selector: 'app-root',
  templateUrl: 'app.component.html',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, TranslateModule, IonApp, IonMenu, IonHeader, IonToolbar, IonTitle, IonContent, IonList, IonMenuToggle, IonItem, IonIcon, IonLabel, IonItemDivider, IonButtons, IonMenuButton, IonButton, IonFooter, IonRouterOutlet
  ],  
})
export class AppComponent {
  isMonetizedMode = false;
  isLoggedIn = false;
  isSuperAdmin = false;
  isAdmin = false;
  showFab: boolean = false;
  pageTitle: string = 'APP.TITLE';
  version = version;

  constructor(private authService: AuthService, 
              private router: Router, 
              private toastController: ToastController, 
              private titleService: TitleService,
              private platform: Platform,
              private modalController: ModalController) {}

  ngOnInit() {
    // Subscribe to authentication status changes
    this.authService.getAuthStatus().subscribe((loggedIn) => {
      this.isLoggedIn = loggedIn;
      this.updateUserRoles();
    });

    this.titleService.title$.subscribe(title => {
      this.pageTitle = title;
    });

    // Monitor navigation changes
    this.router.events.pipe(filter(event => event instanceof NavigationEnd)).subscribe(() => {
      // Refresh authentication state
      this.isLoggedIn = this.authService.isLoggedIn();
      this.updateUserRoles();

      // Global fix for aria-hidden warning
      if (document.activeElement instanceof HTMLElement) {
        document.activeElement.blur();
      }
    });
  }

  get isNative(): boolean {
    return this.platform.is('android') || this.platform.is('ios');
  }

  updateUserRoles() {
    const userRoles = this.authService.getUserRoles();
    this.isSuperAdmin = userRoles.includes("SuperAdmin");
    this.isAdmin = this.isSuperAdmin || userRoles.includes("Admin"); // Admin or SuperAdmin
  }

  openFilters() {
    //console.log("Filter button clicked - Implement filter modal here!");
  }

  logout() {
    this.authService.logout();
    this.isLoggedIn = false;
    this.router.navigate(['/logoff']);
  }

  async openFootballModal() {
    const modal = await this.modalController.create({
      component: ParticipantTournamentsModalComponent,
      breakpoints: [0, 0.5, 0.75, 1],
      initialBreakpoint: 1,
      backdropDismiss: true
    });
    return await modal.present();
  }

  async presentToast(message: string, color: string) {
    const toast = await this.toastController.create({
      message,
      duration: 3000,
      position: 'bottom',
      color,
    });
    await toast.present();
  }

  navigateToMessages(){
    this.router.navigate(['/messages']);
  }

  navigateToProfile() {
    this.router.navigate(['/profile']);
  }

  navigateToNotificationSettings() {
    this.router.navigate(['/notification-settings']);
  }

  navigateToInfoAndSupport(){
    this.router.navigate(['/info-and-support']);
  }

  navigateToMyBets() {
    this.router.navigate(['/my-bets']);
  }

  navigateToMatchInsights() {
    this.router.navigate(['/match-insights']);
  }

  navigateToCreatePredefinedTournament() {
    this.router.navigate(['/tournaments/create-predefined']);
  }

  navigateToPredefinedTournamentsList() {
    this.router.navigate(['/tournaments/predefined']);
  }

  navigateToPredefinedTournamentsMatches() {
    this.router.navigate(['/matches/predefined']);
  }

  navigateToManageUsers(){
    this.router.navigate(['/users']);
  }

  navigateToMyTournaments() {
    this.router.navigate(['/my-tournaments']);
  }

  navigateToCreateCustomTournament() {
    this.router.navigate(['/tournaments/create-custom']);
  }

  navigateToCustomTournamentsList() {
    this.router.navigate(['/tournaments/custom']);
  }

  navigateToCustomTournamentsMatches() {
    this.router.navigate(['/matches/custom']);
  }

  navigateToCustomTournamentsParticipants() {
    this.router.navigate(['/tournaments/participants']);
  }

  navigateToHome() {
    this.router.navigate(['/home']);
  }

  navigateToTournamentResults(){
    this.router.navigate(['/results']);
  }

  navigateToFindTournament() {
    this.router.navigate(['/find-tournament']);
  }  
}
