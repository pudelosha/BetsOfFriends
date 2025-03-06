import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Match } from '../model/match';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root'
})
export class MatchService {
  private apiUrl = `${environment.apiBaseUrl}/matches`;

  constructor(private http: HttpClient) {}

  getMatchesByStatus(tournamentId: number, status: 'Upcoming' | 'InProgress' | 'Finalised'): Observable<Match[]> {
    return this.http.get<Match[]>(`${this.apiUrl}/list/${tournamentId}/${status}`);
  }

  updateMatchResult(matchId: number, matchData: Partial<Match>): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/update/${matchId}`, matchData);
  }
}
