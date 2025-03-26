import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Match } from '../model/match';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root'
})
export class CustomMatchService {
  private apiUrl = `${environment.apiBaseUrl}/custom-matches`;

  constructor(private http: HttpClient) {}

  getMatchesByTournamentStage(tournamentId: number, status: string, stage: string): Observable<Match[]> {
    return this.http.get<Match[]>(`${this.apiUrl}/${tournamentId}/${status}/${stage}`);
  }
  
  updateMatchResult(matchId: number, matchData: Partial<Match>): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/update/${matchId}`, matchData);
  }

  getStartedMatches(tournamentId: number): Observable<Match[]> {
    return this.http.get<Match[]>(`${this.apiUrl}/started/${tournamentId}`);
  }
}
