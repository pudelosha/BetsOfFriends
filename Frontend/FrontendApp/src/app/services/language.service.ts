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
  private supportedLangs = ['en', 'pl', 'de', 'fr', 'es', 'it', 'pt'];

  constructor(private translate: TranslateService, private http: HttpClient) {
    this.translate.addLangs(this.supportedLangs);
    this.translate.setDefaultLang(this.defaultLang);
  }
  
  initLanguage(): void {
    const storedLang = this.getStoredLanguage();
    const browserLang = this.translate.getBrowserLang();
    const lang = this.resolveSupportedLanguage(storedLang || browserLang);
    this.useLanguage(lang);
  }

  useLanguage(lang: string): void {
    const supportedLang = this.resolveSupportedLanguage(lang);
    this.translate.use(supportedLang);
    this.setStoredLanguage(supportedLang);
  }

  updateFromBackend(langFromApi: string): void {
    this.useLanguage(langFromApi);
  }

  get currentLang(): string {
    return this.resolveSupportedLanguage(this.translate.currentLang || this.getStoredLanguage());
  }

  get supportedLanguages(): string[] {
    return [...this.supportedLangs];
  }

  resolveSupportedLanguage(lang?: string | null): string {
    const normalized = this.normalizeLanguage(lang);
    return this.supportedLangs.includes(normalized) ? normalized : this.defaultLang;
  }

  getAvailableLanguages(): Observable<Language[]> {
    return this.http.get<Language[]>(this.apiUrl);
  }

  private normalizeLanguage(lang?: string | null): string {
    const value = (lang || this.defaultLang).trim().toLowerCase();
    return value.split(/[-_]/)[0] || this.defaultLang;
  }

  private getStoredLanguage(): string | null {
    try {
      return localStorage.getItem('lang');
    } catch {
      return null;
    }
  }

  private setStoredLanguage(lang: string): void {
    try {
      localStorage.setItem('lang', lang);
    } catch {
      // Ignore storage failures; language selection should not block registration.
    }
  }
}
