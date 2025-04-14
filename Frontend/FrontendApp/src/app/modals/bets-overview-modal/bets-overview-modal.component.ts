import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ModalController } from '@ionic/angular';
import { IonHeader, IonToolbar, IonTitle, IonButtons, IonButton, IonContent, IonGrid, IonRow, IonCol, IonLabel, IonIcon } from '@ionic/angular/standalone';
import { TranslateModule } from '@ngx-translate/core';
import { BetStats } from 'src/app/model/bet';

@Component({
  selector: 'app-bets-overview-modal',
  templateUrl: './bets-overview-modal.component.html',
  styleUrls: ['./bets-overview-modal.component.scss'],
  standalone: true,
  imports: [CommonModule, TranslateModule, IonHeader, IonToolbar, IonTitle, IonButtons, IonButton, IonContent, IonGrid, IonRow, IonCol, IonIcon],
})
export class BetsOverviewModalComponent {
  @Input() betStats: BetStats = {
    showExactResult: null,
    showQualified: null,
    matchStatus: null,
    homeTeam: '',
    awayTeam: '',
    homeScoreUser: null,
    awayScoreUser: null,
    homeScoreActual: null,
    awayScoreActual: null,
    qualifiedTeam: null,
    percent1: 0,
    percentX: 0,
    percent2: 0,
    percent1Q: null,
    percent2Q: null,
    result: null,
    resultQualified: null
  };

  constructor(private modalCtrl: ModalController) {}

  closeModal() {
    this.modalCtrl.dismiss();
  }

  calculatePlayerColumnSize(): number {
    // Fixed columns: BET(2), HW(1), D(1), AW(1)
    let used = 2 + 1 + 1 + 1;
  
    // Optional columns
    if (this.betStats?.showQualified) used += 2;
    if (this.betStats?.showExactResult) used += 1;
  
    return 12 - used;
  }  
}
