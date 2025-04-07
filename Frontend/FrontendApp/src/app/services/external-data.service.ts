import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';
import { Observable } from 'rxjs';
import { Tournament } from '../model/tournament-model';

@Injectable({
  providedIn: 'root'
})
export class ExternalDataService {
  private apiUrl = `${environment.apiBaseUrl}/externaldata`;

  constructor(private http: HttpClient) {}

  getCompetitionMatches(competitionCode: number, seasonCode: number): Observable<Tournament> {
    const url = `${this.apiUrl}/competition/${competitionCode}/season/${seasonCode}`;
    return this.http.get<Tournament>(url);
  }
}
