import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { HttpClient } from '@angular/common/http';

@Injectable({
  providedIn: 'root'
})
export class TournamentSelectionService {
  private storageKey = 'selectedTournamentId';
  private selectedTournamentSubject: BehaviorSubject<number | null>;

  private apiUrl = `${environment.apiBaseUrl}/tournament-selection`;

  constructor(private http: HttpClient) {
    const storedId = localStorage.getItem(this.storageKey);
    this.selectedTournamentSubject = new BehaviorSubject<number | null>(
      storedId ? parseInt(storedId, 10) : null
    );

    this.loadSelectedTournamentFromBackend();
  }

  setSelectedTournament(tournamentId: number): void {
    localStorage.setItem(this.storageKey, tournamentId.toString());
    this.selectedTournamentSubject.next(tournamentId);

    this.http.post(`${this.apiUrl}/set/${tournamentId}`, {}).subscribe({
      error: (error) => console.error('Error updating selected tournament:', error)
    });
  }

  getSelectedTournament(): number | null {
    return this.selectedTournamentSubject.value;
  }

  loadSelectedTournamentFromBackend(): void {
    this.http.get<{ tournamentId: number | null }>(`${this.apiUrl}/get`).subscribe({
      next: (response) => {
        const tournamentId = response.tournamentId;
        if (tournamentId !== null) {
          localStorage.setItem(this.storageKey, tournamentId.toString());
          this.selectedTournamentSubject.next(tournamentId);
        }
      },
      error: (error) => console.error('Error fetching selected tournament:', error)
    });
  }  

  clearSelectedTournament(): void {
    localStorage.removeItem(this.storageKey);
    this.selectedTournamentSubject.next(null);
    this.setSelectedTournament(-1); // -1 to indicate no selection in backend
  }

  getSelectedTournamentObservable(): Observable<number | null> {
    return this.selectedTournamentSubject.asObservable();
  }
}
