import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import {
  Bet,
  BetUpdateDto,
  BetStats,
  MissingBetsSummary,
  PendingBetReminderSummary,
  SendPendingBetReminderRequest,
  SendPendingBetReminderResult,
  UpcomingBet
} from '../model/bet';

@Injectable({
  providedIn: 'root'
})
export class BetService {
  private apiUrl = `${environment.apiBaseUrl}/bets`;

  constructor(private http: HttpClient) {}

  getBetsByTournamentStage(tournamentId: number, status: string, stage: string): Observable<Bet[]> {
    return this.http.get<Bet[]>(`${this.apiUrl}/${tournamentId}/${status}/${stage}`);
  }
  
  updateBet(betId: number, betUpdate: Partial<BetUpdateDto>): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/update/${betId}`, betUpdate);
  }

  getBetStatsByMatchId(matchId: number): Observable<BetStats> {
    return this.http.get<BetStats>(`${this.apiUrl}/stats/${matchId}`);
  }

  getPendingBetReminders(matchId: number): Observable<PendingBetReminderSummary> {
    return this.http.get<PendingBetReminderSummary>(`${this.apiUrl}/pending-reminders/${matchId}`);
  }

  sendPendingBetReminders(matchId: number, userIds?: string[] | null): Observable<SendPendingBetReminderResult> {
    const request: SendPendingBetReminderRequest = { userIds: userIds ?? null };
    return this.http.post<SendPendingBetReminderResult>(`${this.apiUrl}/pending-reminders/${matchId}/send`, request);
  }

  getUpcomingBets(tournamentId: number): Observable<UpcomingBet[]> {
    return this.http.get<UpcomingBet[]>(`${this.apiUrl}/upcoming/${tournamentId}`);
  }

  getInProgressBets(tournamentId: number): Observable<Bet[]> {
    return this.http.get<Bet[]>(`${this.apiUrl}/in-progress/${tournamentId}`);
  }

  getMissingBets(tournamentId: number): Observable<MissingBetsSummary> {
    return this.http.get<MissingBetsSummary>(`${this.apiUrl}/missing/${tournamentId}`);
  }
}
