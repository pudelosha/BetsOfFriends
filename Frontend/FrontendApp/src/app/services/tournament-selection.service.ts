import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class TournamentSelectionService {
  private storageKey = 'selectedTournamentId';
  private selectedTournamentSubject: BehaviorSubject<number | null>;

  constructor() {
    // Initialize Subject with value from localStorage (if available)
    const storedId = localStorage.getItem(this.storageKey);
    this.selectedTournamentSubject = new BehaviorSubject<number | null>(
      storedId ? parseInt(storedId, 10) : null
    );
  }

  /** Stores selected tournament ID and updates the Subject */
  setSelectedTournament(tournamentId: number): void {
    localStorage.setItem(this.storageKey, tournamentId.toString());
    this.selectedTournamentSubject.next(tournamentId); // Notify all subscribers
  }

  /** Retrieves selected tournament ID */
  getSelectedTournament(): number | null {
    return this.selectedTournamentSubject.value;
  }

  /** Clears selected tournament from localStorage and Subject */
  clearSelectedTournament(): void {
    localStorage.removeItem(this.storageKey);
    this.selectedTournamentSubject.next(null);
  }

  /** Returns an observable for components to subscribe to real-time updates */
  getSelectedTournamentObservable() {
    return this.selectedTournamentSubject.asObservable();
  }
}
