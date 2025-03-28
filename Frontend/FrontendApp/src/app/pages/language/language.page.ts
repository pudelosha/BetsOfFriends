import { Component, CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { IonContent, IonPicker, IonButton } from '@ionic/angular/standalone';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';

@Component({
  selector: 'app-language',
  templateUrl: './language.page.html',
  styleUrls: ['./language.page.scss'],
  standalone: true,
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
  imports: [CommonModule, IonContent, IonPicker, IonButton]
})
export class LanguagePage {
  parallaxOffset = 0;
  showPicker = false;
  selectedLang = 'en';

  translations: Record<string, { continue: string; heading: string }> = {
    en: { continue: 'Continue', heading: 'Select Your Language' },
    fr: { continue: 'Continuer', heading: 'Choisissez votre langue' },
    de: { continue: 'Weiter', heading: 'Sprache auswählen' },
    es: { continue: 'Continuar', heading: 'Selecciona tu idioma' },
    it: { continue: 'Continua', heading: 'Seleziona la tua lingua' },
    pt: { continue: 'Continuar', heading: 'Selecione o seu idioma' },
    pl: { continue: 'Kontynuuj', heading: 'Wybierz swój język' },
    ru: { continue: 'Продолжить', heading: 'Выберите язык' },
    uk: { continue: 'Продовжити', heading: 'Оберіть мову' },
    tr: { continue: 'Devam et', heading: 'Dil seçin' },
    ar: { continue: 'استمر', heading: 'اختر لغتك' },
    zh: { continue: '继续', heading: '选择你的语言' },
    hi: { continue: 'जारी रखें', heading: 'अपनी भाषा चुनें' }
  };
  
  constructor(private router: Router) {}

  onScroll(event: any) {
    this.parallaxOffset = event.detail.scrollTop * 0.5;
  }

  onLangChange(event: CustomEvent) {
    this.selectedLang = event.detail.value;
    this.showPicker = false;
  }
  
  get continueLabel(): string {
    return this.translations[this.selectedLang]?.continue || 'Continue';
  }
  
  get headingLabel(): string {
    return this.translations[this.selectedLang]?.heading || 'Select Your Language';
  }
  

  onContinue() {
    console.log('Selected language:', this.selectedLang);
    this.router.navigate(['/welcome']);
  }

  getSelectedLanguageLabel(): string {
    const labels: Record<string, string> = {
      en: 'English',
      fr: 'Français',
      de: 'Deutsch',
      es: 'Español',
      it: 'Italiano',
      pt: 'Português',
      pl: 'Polski',
      ru: 'Русский',
      uk: 'Українська',
      tr: 'Türkçe',
      ar: 'العربية',
      zh: '中文',
      hi: 'हिन्दी'
    };
    return labels[this.selectedLang] || 'Select Language';
  }
}
