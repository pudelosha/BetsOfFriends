import { Component, Input, OnInit } from '@angular/core';
import { IonicModule, ModalController } from '@ionic/angular';
import { ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { Tournament } from 'src/app/model/tournament-model';
import { TranslateModule } from '@ngx-translate/core';


@Component({
  selector: 'app-tournament-selection-modal',
  templateUrl: './tournament-selection-modal.html',
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule, TranslateModule],
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
