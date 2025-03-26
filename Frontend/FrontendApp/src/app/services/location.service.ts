import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class LocationService {
  private apiUrl = `${environment.apiBaseUrl}/locations`;

  constructor(private http: HttpClient) {}

  getAvailableCountries() {
    return this.http.get<{ countryId: number, name: string }[]>(`${this.apiUrl}`);
  }
}
