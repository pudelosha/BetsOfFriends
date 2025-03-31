import { Component, CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { IonContent, IonPicker, IonButton } from '@ionic/angular/standalone';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { LanguageService } from 'src/app/services/language.service';

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

  constructor(private router: Router, private languageService: LanguageService) {}

  onScroll(event: any) {
    this.parallaxOffset = event.detail.scrollTop * 0.5;
  }

  onLangChange(event: CustomEvent) {
    this.selectedLang = event.detail.value;
    this.showPicker = false;
  }

  onContinue() {
    this.languageService.useLanguage(this.selectedLang);
    console.log('Language saved:', this.selectedLang);
    this.router.navigate(['/welcome']);
  }

  get continueLabel(): string {
    const labelMap: Record<string, string> = {
      en: 'Continue',
      fr: 'Continuer',
      de: 'Weiter',
      es: 'Continuar',
      it: 'Continua',
      pt: 'Continuar',
      pl: 'Kontynuuj',
      ru: 'Продолжить',
      uk: 'Продовжити',
      tr: 'Devam et',
      ar: 'استمر',
      zh: '继续',
      hi: 'जारी रखें'
    };
    return labelMap[this.selectedLang] || 'Continue';
  }

  get headingLabel(): string {
    const headingMap: Record<string, string> = {
      en: 'Select Your Language',
      fr: 'Choisissez votre langue',
      de: 'Sprache auswählen',
      es: 'Selecciona tu idioma',
      it: 'Seleziona la tua lingua',
      pt: 'Selecione o seu idioma',
      pl: 'Wybierz swój język',
      ru: 'Выберите язык',
      uk: 'Оберіть мову',
      tr: 'Dil seçin',
      ar: 'اختر لغتك',
      zh: '选择你的语言',
      hi: 'अपनी भाषा चुनें'
    };
    return headingMap[this.selectedLang] || 'Select Your Language';
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
