import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { Bet, BetUpdateDto, BetStats, UpcomingBet } from '../model/bet';

@Injectable({
  providedIn: 'root'
})
export class BetService {
  private apiUrl = `${environment.apiBaseUrl}/bets`;

  constructor(private http: HttpClient) {}

  getBetsByStatus(tournamentId: number, status: 'ToPlace' | 'Placed' | 'Finalised'): Observable<Bet[]> {
    return this.http.get<Bet[]>(`${this.apiUrl}/list/${tournamentId}/${status}`);
  }
  
  updateBet(betId: number, betUpdate: Partial<BetUpdateDto>): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/update/${betId}`, betUpdate);
  }

  getBetStatsByMatchId(matchId: number): Observable<BetStats> {
    return this.http.get<BetStats>(`${this.apiUrl}/stats/${matchId}`);
  }

  getUpcomingBets(tournamentId: number): Observable<UpcomingBet[]> {
    return this.http.get<UpcomingBet[]>(`${this.apiUrl}/upcoming/${tournamentId}`);
  }
}
