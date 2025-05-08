import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Tournament } from '../model/tournament-model';
import { environment } from '../../environments/environment';
import { UserActiveTournament, TournamentSummary, TournamentPlayerResult, TournamentInvite, UserBettingStats, PublicTournament, TournamentParticipant, SelectedTournamentDetails } from '../model/tournament-model';
import { ActionResult } from '../model/action-result';
import { map, catchError } from 'rxjs/operators';
import { of } from 'rxjs';


@Injectable({
  providedIn: 'root'
})
export class CustomTournamentService {
  private apiUrl = `${environment.apiBaseUrl}/custom-tournaments`;

  constructor(private http: HttpClient) {}

  getCustomTournamentById(id: number): Observable<Tournament> {
    return this.http.get<Tournament>(`${this.apiUrl}/get/${id}`);
  }

  getUserActiveTournaments(): Observable<UserActiveTournament[]> {
    return this.http.get<UserActiveTournament[]>(`${this.apiUrl}/my-active-tournaments`);
  }

  createCustomTournament(tournament: Tournament): Observable<Tournament> {
    return this.http.post<Tournament>(`${this.apiUrl}/create`, tournament);
  }

  updateCustomTournament(tournament: Tournament): Observable<any> {
    return this.http.put(`${this.apiUrl}/update`, tournament, { responseType: 'text' });
  }

  checkForTournamentUpdates(tournamentId: number): Observable<Tournament> {
    return this.http.get<Tournament>(`${this.apiUrl}/pending-updates/${tournamentId}`);
  }
  
  getCustomTournaments(): Observable<Tournament[]> {
    return this.http.get<Tournament[]>(`${this.apiUrl}`);
  } 

  getActiveCustomTournaments(): Observable<Tournament[]> {
    return this.http.get<Tournament[]>(`${this.apiUrl}/active`);
  }
  
  updateCustomTournamentStatus(tournamentId: number, isActive: boolean): Observable<void> {
    const requestBody = { isActive };
    return this.http.patch<void>(`${this.apiUrl}/status/${tournamentId}`, requestBody);
  }

  deleteCustomTournament(tournamentId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/delete/${tournamentId}`);
  }

  quitTournament(tournamentId: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/quit/${tournamentId}`);
  }  

  acceptTournamentInvitation(tournamentId: number, nickname: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/accept-invitation/${tournamentId}`, { nickname });
  }  

  toggleTournamentVisibility(tournamentId: number): Observable<boolean> {
    return this.http.patch<boolean>(`${this.apiUrl}/visibility/${tournamentId}`, {});
  }

  recalculateBetsForTournament(tournamentId: number): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/recalculate/${tournamentId}`, {});
  }  

  getTournamentSummary(tournamentId: number): Observable<TournamentSummary[]> {
    return this.http.get<TournamentSummary[]>(`${this.apiUrl}/summary/${tournamentId}`);
  } 
  
  getTournamentPlayerResult(tournamentId: number): Observable<TournamentPlayerResult[]> {
    return this.http.get<TournamentPlayerResult[]>(`${this.apiUrl}/result/${tournamentId}`);
  }

  getPendingTournamentInvites(): Observable<TournamentInvite[]> {
    return this.http.get<TournamentInvite[]>(`${this.apiUrl}/invites/pending`);
  }

  getTournamentStages(tournamentId: number): Observable<string[]> {
    return this.http.get<string[]>(`${this.apiUrl}/stages/${tournamentId}`);
  } 

  getFirstStageWithPendingBets(tournamentId: number): Observable<string | null> {
    return this.http.get(`${this.apiUrl}/pending-stage/${tournamentId}`, {
      responseType: 'text',
      observe: 'response'
    }).pipe(
      map(response => {
        if (response.status === 204) return null;
        return response.body ?? null;
      }),
      catchError(err => {
        console.error('Error fetching first stage with pending bets:', err);
        return of(null);
      })
    );
  }
  
  getFirstStageWithUpcomingMatches(tournamentId: number): Observable<string | null> {
    return this.http.get(`${this.apiUrl}/upcoming-stage/${tournamentId}`, {
      responseType: 'text',
      observe: 'response'
    }).pipe(
      map(response => {
        if (response.status === 204) return null;
        return response.body ?? null;
      }),
      catchError(err => {
        console.error('Error fetching first stage with upcoming matches:', err);
        return of(null);
      })
    );
  }
          
  getUserBettingStats(tournamentId: number, statsUserId: string): Observable<UserBettingStats[]> {
    return this.http.get<UserBettingStats[]>(`${this.apiUrl}/betting-stats/${tournamentId}/${statsUserId}`);
  } 
  
  checkTournamentNameAvailability(name: string, visibility: string): Observable<{ available: boolean }> {
    return this.http.post<{ available: boolean }>(`${this.apiUrl}/check-name`, { name, visibility });
  }

  searchPublicTournaments(searchTerm: string = ''): Observable<PublicTournament[]> {
    return this.http.post<PublicTournament[]>(`${this.apiUrl}/search-public`, { searchTerm: searchTerm.trim() });
  }

  requestToJoinTournament(tournamentId: number, nickname: string, message: string): Observable<any> {
    const body = {
      tournamentId,
      nickname,
      message
    };

    return this.http.post(`${this.apiUrl}/request-join`, body);
  }

  getTournamentParticipants(tournamentId: number, status: string = 'Accepted') { 
    return this.http.get<TournamentParticipant[]>(`${this.apiUrl}/participants/${tournamentId}?status=${status}`);
  }
  
  excludeParticipant(tournamentId: number, userEmail: string): Observable<ActionResult> {
    return this.http.post<ActionResult>(`${this.apiUrl}/participants/${tournamentId}/exclude`, { userEmail });
  }
  
  acceptParticipant(tournamentId: number, userEmail: string): Observable<ActionResult> {
    return this.http.post<ActionResult>(`${this.apiUrl}/participants/${tournamentId}/accept`, { userEmail });
  }
  
  resendParticipantInvite(tournamentId: number, userEmail: string): Observable<ActionResult> {
    return this.http.post<ActionResult>(`${this.apiUrl}/participants/${tournamentId}/resend`, { userEmail });
  } 

  getAssignmentDetails(tournamentId: number): Observable<{ nickname: string }> {
    return this.http.get<{ nickname: string }>(`${this.apiUrl}/assignment/${tournamentId}`);
  }

  updateTournamentAssignment(tournamentId: number, nickname: string): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(
      `${this.apiUrl}/assignment/${tournamentId}`,
      { nickname }
    );
  }

  getSelectedTournamentDetails(tournamentId: number): Observable<SelectedTournamentDetails> {
    return this.http.get<SelectedTournamentDetails>(`${this.apiUrl}/details/${tournamentId}`);
  }
}
