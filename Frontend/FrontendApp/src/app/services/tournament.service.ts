import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Tournament } from '..//model/tournament-model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class PredefinedTournamentService {
  private apiUrl = `${environment.apiBaseUrl}/predefined`;

  constructor(private http: HttpClient) {}

  getPredefinedTournamentById(id: number): Observable<Tournament> {
    return this.http.get<Tournament>(`${this.apiUrl}/get-tournament/${id}`);
  }

  createPredefinedTournament(tournament: Tournament): Observable<Tournament> {
    return this.http.post<Tournament>(`${this.apiUrl}/create-tournament`, tournament);
  }

  updatePredefinedTournament(tournament: Tournament): Observable<Tournament> {
    return this.http.put<Tournament>(`${this.apiUrl}/update-tournament/${tournament.id}`, tournament);
  }
}
