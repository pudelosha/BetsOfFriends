import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LanguageService } from 'src/app/services/language.service';
import { IonFab, IonFabButton, IonFabList } from '@ionic/angular/standalone';

@Component({
  selector: 'app-language-fab',
  standalone: true,
  imports: [CommonModule, IonFab, IonFabButton, IonFabList],
  template: `
    <ion-fab vertical="bottom" horizontal="end" slot="fixed">
      <ion-fab-button size="small">🌐</ion-fab-button>
      <ion-fab-list side="top">
        <ion-fab-button
          *ngFor="let lang of supportedLangs"
          size="small"
          (click)="switchLang(lang)"
        >
          {{ lang.toUpperCase() }}
        </ion-fab-button>
      </ion-fab-list>
    </ion-fab>
  `
})
export class LanguageFabComponent {
  @Input() supportedLangs: string[] = [
    'en', 'pl', 'de', 'fr', 'es', 'it', 'pt'
  ];

  constructor(private languageService: LanguageService) {}

  switchLang(lang: string) {
    this.languageService.useLanguage(lang);
  }
}
