import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';


@Injectable({
  providedIn: 'root'
})
export class DownloadService {
  private apiUrl = `${environment.apiBaseUrl}/downloads`;

  constructor() {}

  downloadFile(fileName: string): void {
    const url = `${this.apiUrl}/${fileName}`;
    window.open(url, '_blank'); // Trigger file download in new tab
  }

  openStore(platform: 'ios' | 'android'): void {
    const storeUrls = {
      ios: 'https://apps.apple.com/',
      android: 'https://play.google.com/store'
    };

    window.open(storeUrls[platform], '_blank');
  }
}
