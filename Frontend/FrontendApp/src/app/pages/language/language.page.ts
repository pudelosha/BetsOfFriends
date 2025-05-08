import { Component, CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { LanguageService } from 'src/app/services/language.service';
import { Language } from 'src/app/model/language';
import { IonContent, IonPicker, IonPickerColumn, IonPickerColumnOption, IonButton } from '@ionic/angular/standalone';

@Component({
  selector: 'app-language',
  templateUrl: './language.page.html',
  styleUrls: ['./language.page.scss'],
  standalone: true,
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
  imports: [CommonModule, IonContent, IonPicker, IonPickerColumn, IonPickerColumnOption, IonButton]
})
export class LanguagePage {
  parallaxOffset = 0;
  showPicker = false;
  selectedLang = 'en';
  availableLanguages: Language[] = [];


  constructor(private router: Router, private languageService: LanguageService) {}

  ngOnInit(): void {
    this.languageService.getAvailableLanguages().subscribe((languages) => {
      this.availableLanguages = languages;
    });
  }

  onScroll(event: any) {
    this.parallaxOffset = event.detail.scrollTop * 0.5;
  }

  onLangChange(event: CustomEvent) {
    this.selectedLang = event.detail.value;
    this.showPicker = false;
  }

  onContinue() {
    this.languageService.useLanguage(this.selectedLang);
    //console.log('Language saved:', this.selectedLang);
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
      pl: 'Kontynuuj'
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
      pl: 'Wybierz swój język'
    };
    return headingMap[this.selectedLang] || 'Select Your Language';
  }

  getSelectedLanguageLabel(): string {
    const found = this.availableLanguages.find(l => l.shortName === this.selectedLang);
    return found?.longName || 'Select Language';
  }
}
