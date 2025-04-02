import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonicModule } from '@ionic/angular';
import { ModalController } from '@ionic/angular';
import { BetStats } from 'src/app/model/bet';
import { TranslateModule } from '@ngx-translate/core';


@Component({
  selector: 'app-bets-overview-modal',
  templateUrl: './bets-overview-modal.component.html',
  styleUrls: ['./bets-overview-modal.component.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, TranslateModule],
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
}
