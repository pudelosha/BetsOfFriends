import { bootstrapApplication } from '@angular/platform-browser';
import { RouteReuseStrategy, provideRouter, withPreloading, PreloadAllModules } from '@angular/router';
import { IonicRouteStrategy, provideIonicAngular } from '@ionic/angular/standalone';
import { provideHttpClient, withInterceptorsFromDi  } from '@angular/common/http';
import { importProvidersFrom } from '@angular/core';
import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { AuthInterceptor } from './app/interceptors/auth.interceptor';

import { routes } from './app/app.routes';
import { AppComponent } from './app/app.component';

import { addIcons } from 'ionicons';
import { logOutOutline, homeOutline, trophyOutline, searchOutline, chatbubblesOutline, cogOutline, informationCircleOutline, pauseOutline, trashOutline, settingsOutline, listOutline, podiumOutline, statsChartOutline, footballOutline, createOutline, eyeOutline, personOutline, notificationsOutline, shieldCheckmarkOutline, addCircleOutline, hammerOutline, clipboardOutline, starOutline, documentTextOutline, buildOutline, exitOutline, filterOutline } from 'ionicons/icons';

bootstrapApplication(AppComponent, {
  providers: [
    { provide: RouteReuseStrategy, useClass: IonicRouteStrategy },
    provideIonicAngular(),
    provideRouter(routes, withPreloading(PreloadAllModules)),
    provideHttpClient(withInterceptorsFromDi()),
    { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true }
  ],
});

addIcons({
  'chatbubbles-outline': chatbubblesOutline,
  'cog-outline': cogOutline,
  'information-circle-outline': informationCircleOutline,
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
