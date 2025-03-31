import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonicModule } from '@ionic/angular';
import { LanguageService } from 'src/app/services/language.service';

@Component({
  selector: 'app-language-fab',
  standalone: true,
  imports: [CommonModule, IonicModule],
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
    'en', 'fr', 'pl', 'de', 'es', 'it', 'pt', 'ru', 'uk', 'tr', 'ar', 'zh', 'hi'
  ];

  constructor(private languageService: LanguageService) {}

  switchLang(lang: string) {
    this.languageService.useLanguage(lang);
  }
}
