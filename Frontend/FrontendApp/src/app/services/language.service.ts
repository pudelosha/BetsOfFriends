import { Injectable } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { environment } from 'src/environments/environment';
import { HttpClient } from '@angular/common/http';
import { Language } from '../model/language';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class LanguageService {
  private apiUrl = `${environment.apiBaseUrl}/languages`;
  
  private defaultLang = 'en';

  constructor(private translate: TranslateService, private http: HttpClient) {
    this.translate.addLangs([
      'en', 'fr', 'pl', 'de', 'es', 'zh', 'ar', 'hi', 'it', 'pt', 'ru', 'uk', 'tr']);
    this.translate.setDefaultLang(this.defaultLang);
  }

  initLanguage(): void {
    const storedLang = localStorage.getItem('lang');
    const browserLang = this.translate.getBrowserLang();
    const lang = storedLang || browserLang || this.defaultLang;
    this.useLanguage(lang);
  }

  useLanguage(lang: string): void {
    this.translate.use(lang);
    localStorage.setItem('lang', lang);
  }

  updateFromBackend(langFromApi: string): void {
    this.useLanguage(langFromApi);
  }

  get currentLang(): string {
    return this.translate.currentLang;
  }

  getAvailableLanguages(): Observable<Language[]> {
    return this.http.get<Language[]>(this.apiUrl);
  }
}
