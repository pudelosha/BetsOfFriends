import { Component, OnInit, ViewChild  } from '@angular/core';
import { Router, NavigationEnd  } from '@angular/router';
import { AuthService } from './services/auth.service';
import { ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { filter } from 'rxjs/operators';
import { ToastController, ModalController, MenuController } from '@ionic/angular';
import { ParticipantTournamentsModalComponent } from './modals/participant-tournaments-modal/participant-tournaments-modal.component';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { TitleService } from './services/title.service';
import { Platform } from '@ionic/angular';
import { IonMenu, IonApp, IonHeader, IonToolbar, IonTitle, IonRow, IonGrid, IonCol, IonContent, IonList, IonMenuToggle, IonItem, IonIcon, IonLabel, IonItemDivider, IonButtons, IonMenuButton, IonButton, IonFooter, IonRouterOutlet, IonFab, IonFabButton, IonFabList } from '@ionic/angular/standalone';
import { version } from 'src/environments/version';
import { LanguageService } from 'src/app/services/language.service';
import { UserService } from 'src/app/services/user.service';
import { firstValueFrom } from 'rxjs';
import { TournamentSelectionService } from './services/tournament-selection.service';
import { CustomTournamentService } from './services/custom-tournament.service';

@Component({
  selector: 'app-root',
  templateUrl: 'app.component.html',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TranslateModule, IonApp, IonMenu, IonRow, IonGrid, IonCol, IonHeader, IonToolbar, IonTitle, IonContent, IonList, IonMenuToggle, IonItem, IonIcon, IonLabel, IonItemDivider, IonButtons, IonMenuButton, IonButton, IonFooter, IonRouterOutlet],  
})
export class AppComponent implements OnInit {
  @ViewChild('sideMenuContent') sideMenuContent?: IonContent;

  isMonetizedMode = false;
  isLoggedIn = false;
  isSuperAdmin = false;
  isAdmin = false;
  selectedTournamentUpdateMethod: 'Manual' | 'Semi' | 'Auto' | null = null;
  showFab: boolean = false;
  pageTitle: string = 'APP.TITLE';
  version = version;

  supportedLangs = ['en', 'pl', 'de', 'fr', 'es', 'it', 'pt'];

  constructor(private authService: AuthService, 
              private router: Router, 
              private toastController: ToastController, 
              private titleService: TitleService,
              private platform: Platform,
              private languageService: LanguageService,
              private userService: UserService,
              private tournamentSelectionService: TournamentSelectionService,
              private customTournamentService: CustomTournamentService,
              private translate: TranslateService,
              private menuController: MenuController,
              private modalController: ModalController) {}

  ngOnInit() {
    this.languageService.initLanguage();

    // Subscribe to authentication status changes
    this.authService.getAuthStatus().subscribe((loggedIn) => {
      this.isLoggedIn = loggedIn;
      this.updateUserRoles();
      if (loggedIn) {
        void this.syncLanguageFromProfile();
        this.loadSelectedTournamentUpdateMethod();
      }
    });

    this.titleService.title$.subscribe(title => {
      this.pageTitle = title;
    });

    // Monitor navigation changes
    this.router.events.pipe(filter(event => event instanceof NavigationEnd)).subscribe(() => {
      // Refresh authentication state
      this.isLoggedIn = this.authService.isLoggedIn();
      this.updateUserRoles();
      if (this.isLoggedIn) {
        this.loadSelectedTournamentUpdateMethod();
      }

      // Global fix for aria-hidden warning
      if (document.activeElement instanceof HTMLElement) {
        document.activeElement.blur();
      }
    });

    this.tournamentSelectionService.getSelectedTournamentObservable().subscribe(() => {
      if (this.authService.isLoggedIn()) {
        this.loadSelectedTournamentUpdateMethod();
      } else {
        this.selectedTournamentUpdateMethod = null;
      }
    });
  }

  switchLang(lang: string) {
    this.languageService.useLanguage(lang);
  }

  private async syncLanguageFromProfile(): Promise<void> {
    const tokenAtStart = this.authService.getToken();
    if (!tokenAtStart) {
      return;
    }

    try {
      const profile = await firstValueFrom(this.userService.getUserProfile());
      const profileLanguage = profile?.language?.trim();

      if (profileLanguage && this.authService.getToken() === tokenAtStart) {
        this.languageService.updateFromBackend(profileLanguage);
      }
    } catch (error) {
      console.warn('Failed to sync language from user profile:', error);
    }
  }

  get isNative(): boolean {
    return this.platform.is('android') || this.platform.is('ios');
  }

  get isCustomMatchesMenuLocked(): boolean {
    return this.selectedTournamentUpdateMethod === 'Auto';
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

  async openLanguageModal() {
    
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

  onMenuWillOpen() {
    setTimeout(() => {
      void this.sideMenuContent?.scrollToTop(0);
    }, 0);
  }

  private loadSelectedTournamentUpdateMethod(): void {
    const tournamentId = this.tournamentSelectionService.getSelectedTournament();
    if (!tournamentId || tournamentId < 0) {
      this.selectedTournamentUpdateMethod = null;
      return;
    }

    this.customTournamentService.getSelectedTournamentDetails(tournamentId).subscribe({
      next: details => {
        this.selectedTournamentUpdateMethod = details.updateMethod ?? null;
      },
      error: error => {
        console.warn('Failed to load selected tournament update method:', error);
        this.selectedTournamentUpdateMethod = null;
      }
    });
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

  async navigateToCustomTournamentsMatches(event?: Event) {
    event?.preventDefault();
    event?.stopPropagation();

    const tournamentId = this.tournamentSelectionService.getSelectedTournament();
    if (!tournamentId || tournamentId < 0) {
      await this.menuController.close();
      await this.router.navigate(['/matches/custom']);
      return;
    }

    try {
      const details = await firstValueFrom(this.customTournamentService.getSelectedTournamentDetails(tournamentId));
      this.selectedTournamentUpdateMethod = details.updateMethod ?? null;

      if ((details.updateMethod ?? '').toLowerCase() === 'auto') {
        await this.menuController.close();
        await this.presentToast(this.translate.instant('TOASTS.AUTO_UPDATE_MATCHES_LOCKED'), 'warning');
        return;
      }

      await this.menuController.close();
      await this.router.navigate(['/matches/custom']);
    } catch (error) {
      console.warn('Failed to verify selected tournament update method:', error);
      await this.menuController.close();
      await this.router.navigate(['/matches/custom']);
    }
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

  navigateToExtraPredictions() {
    this.router.navigate(['/extra-predictions']);
  }

  navigateToFindTournament() {
    this.router.navigate(['/find-tournament']);
  }  
}
