import { Component, Input } from '@angular/core'; 
import { CommonModule } from '@angular/common'; 
import { ModalController } from '@ionic/angular'; 
import { IonHeader, IonToolbar, IonTitle, IonButtons, IonButton, IonContent, IonList, IonItem, IonLabel } from '@ionic/angular/standalone'; 
import { TranslateModule } from '@ngx-translate/core'; 
import { Tournament } from 'src/app/model/tournament-model';

@Component({
  selector: 'app-tournament-selection-modal',
  templateUrl: './tournament-selection-modal.html',
  standalone: true,
  imports: [CommonModule, TranslateModule, IonHeader, IonToolbar, IonTitle, IonButtons, IonButton, IonContent, IonList, IonItem, IonLabel],
})
export class TournamentSelectionModalComponent {
  @Input() predefinedTournaments: Tournament[] = [];

  constructor(private modalController: ModalController) {}

  selectTournament(tournamentId: any): void {
    this.modalController.dismiss({ selectedTournamentId: tournamentId });
  }

  dismiss(): void {
    this.modalController.dismiss({ selectedTournamentId: null });
  }
}
