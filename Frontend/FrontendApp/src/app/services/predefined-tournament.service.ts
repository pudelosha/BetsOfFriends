import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Tournament } from '../model/tournament-model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class PredefinedTournamentService {
  private apiUrl = `${environment.apiBaseUrl}/predefined-tournaments`;

  constructor(private http: HttpClient) {}

  getPredefinedTournamentById(id: number): Observable<Tournament> {
    return this.http.get<Tournament>(`${this.apiUrl}/get/${id}`);
  }

  createPredefinedTournament(tournament: Tournament): Observable<Tournament> {
    return this.http.post<Tournament>(`${this.apiUrl}/create`, tournament);
  }

  updatePredefinedTournament(tournament: Tournament): Observable<any> {
    return this.http.put(`${this.apiUrl}/update`, tournament, { responseType: 'text' });
  }

  getPredefinedTournaments(): Observable<Tournament[]> {
    return this.http.get<Tournament[]>(`${this.apiUrl}`);
  } 

  getActivePredefinedTournaments(): Observable<Tournament[]> {
    return this.http.get<Tournament[]>(`${this.apiUrl}/active`);
  }
  
  updatePredefinedTournamentStatus(tournamentId: number, isActive: boolean): Observable<void> {
    const requestBody = { isActive };
    return this.http.patch<void>(`${this.apiUrl}/status/${tournamentId}`, requestBody);
  }

  deletePredefinedTournament(tournamentId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/delete/${tournamentId}`);
  }

  getTournamentStages(tournamentId: number): Observable<string[]> {
    return this.http.get<string[]>(`${this.apiUrl}/stages/${tournamentId}`);
  } 
}
