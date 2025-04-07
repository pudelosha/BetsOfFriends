import { bootstrapApplication } from '@angular/platform-browser';
import { RouteReuseStrategy, provideRouter, withPreloading, PreloadAllModules } from '@angular/router';
import { IonicRouteStrategy, provideIonicAngular } from '@ionic/angular/standalone';
import { provideHttpClient, withInterceptorsFromDi  } from '@angular/common/http';
import { importProvidersFrom } from '@angular/core';
import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { AuthInterceptor } from './app/interceptors/auth.interceptor';
import { provideTranslation } from './app/config/translation.config';

import { routes } from './app/app.routes';
import { AppComponent } from './app/app.component';

import { addIcons } from 'ionicons';
import { logOutOutline, homeOutline, logoAndroid, logoX, logoTiktok, logoApple, logoTwitter, logoFacebook, logoDiscord, logoInstagram, logoYoutube, helpBuoyOutline, shareSocialOutline, helpCircleOutline, cloudDownloadOutline, logoGooglePlaystore, downloadOutline, documentAttachOutline, trophyOutline, checkmark, trash, close, mailOutline, peopleOutline, chevronBackOutline, chevronForwardOutline, searchOutline, chatbubblesOutline, cogOutline, informationCircleOutline, pauseOutline, trashOutline, settingsOutline, listOutline, podiumOutline, statsChartOutline, footballOutline, createOutline, eyeOutline, personOutline, notificationsOutline, shieldCheckmarkOutline, addCircleOutline, hammerOutline, clipboardOutline, starOutline, documentTextOutline, buildOutline, exitOutline, filterOutline } from 'ionicons/icons';

bootstrapApplication(AppComponent, {
  providers: [
    { provide: RouteReuseStrategy, useClass: IonicRouteStrategy },
    provideIonicAngular(),
    provideTranslation(),
    provideRouter(routes, withPreloading(PreloadAllModules)),
    provideHttpClient(withInterceptorsFromDi()),
    { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true }
  ],
});

addIcons({
  'logo-x': logoX,
  'logo-tiktok': logoTiktok,
  'logo-twitter': logoTwitter,
  'logo-facebook': logoFacebook,
  'logo-discord': logoDiscord,
  'logo-instagram': logoInstagram,
  'logo-youtube': logoYoutube,
  'information-circle-outline': informationCircleOutline,
  'share-social-outline': shareSocialOutline,
  'help-buoy-outline': helpBuoyOutline,
  'help-circle-outline': helpCircleOutline,
  'cloud-download-outline': cloudDownloadOutline,
  'logo-google-playstore': logoGooglePlaystore,
  'download-outline': downloadOutline,
  'logo-apple': logoApple,
  'document-attach-outline': documentAttachOutline,
  'logo-android': logoAndroid,
  'close': close,
  'checkmark': checkmark,
  'people-outline': peopleOutline,
  'mail-outline': mailOutline,
  'trash': trash,
  'chevron-forward-outline': chevronForwardOutline,
  'chevron-back-outline': chevronBackOutline,
  'chatbubbles-outline': chatbubblesOutline,
  'cog-outline': cogOutline,
  'search-outline': searchOutline,
  'trash-outline': trashOutline,
  'pause-outline': pauseOutline,
  'filter-outline': filterOutline,
  'log-out-outline': logOutOutline,
  'home-outline': homeOutline,
  'trophy-outline': trophyOutline,
  'settings-outline': settingsOutline,
  'list-outline': listOutline,
  'podium-outline': podiumOutline,
  'stats-chart-outline': statsChartOutline,
  'football-outline': footballOutline,
  'create-outline': createOutline,
  'eye-outline': eyeOutline,
  'person-outline': personOutline,
  'notifications-outline': notificationsOutline,
  'shield-checkmark-outline': shieldCheckmarkOutline,
  'add-circle-outline': addCircleOutline,
  'hammer-outline': hammerOutline,
  'clipboard-outline': clipboardOutline,
  'star-outline': starOutline,
  'document-text-outline': documentTextOutline,
  'build-outline': buildOutline,
  'exit-outline': exitOutline
});
