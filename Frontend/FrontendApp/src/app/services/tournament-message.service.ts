import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { TournamentMessage, CreateMessageResult } from '../model/message';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root'
})
export class TournamentMessageService {

  private apiUrl = `${environment.apiBaseUrl}/tournament-messages`;

  constructor(private http: HttpClient) { }

  getMessages(tournamentId: number): Observable<TournamentMessage[]> {
    return this.http.get<TournamentMessage[]>(`${this.apiUrl}/${tournamentId}`);
  }

  postMessage(tournamentId: number, content: string): Observable<CreateMessageResult> {
    return this.http.post<CreateMessageResult>(`${this.apiUrl}/${tournamentId}`, { content });
  }
}
