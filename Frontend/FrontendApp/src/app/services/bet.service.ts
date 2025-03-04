import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { Bet } from '../model/bet';

@Injectable({
  providedIn: 'root'
})
export class BetService {
  private apiUrl = `${environment.apiBaseUrl}/bets`;

  constructor(private http: HttpClient) {}

  getBetsByStatus(tournamentId: number, status: 'ToPlace' | 'Placed' | 'Finalised'): Observable<Bet[]> {
    return this.http.get<Bet[]>(`${this.apiUrl}/list/${tournamentId}/${status}`);
  }
  
}
