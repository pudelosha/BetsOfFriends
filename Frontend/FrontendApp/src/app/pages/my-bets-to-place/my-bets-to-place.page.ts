import { Component, OnInit } from '@angular/core';
import { ModalController } from '@ionic/angular';
import { EditBetModalComponent } from '..//../modals/edit-bet-modal/edit-bet-modal.component';
import { ReactiveFormsModule, FormsModule  } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { IonicModule } from '@ionic/angular';
import { Bet } from '..//../model/bet';

@Component({
  selector: 'app-my-bets-to-place',
  templateUrl: './my-bets-to-place.page.html',
  styleUrls: ['./my-bets-to-place.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule, FormsModule],
})
export class MyBetsToPlacePage implements OnInit {
  showFabButton: boolean = true; // Control FAB visibility

  bets: Bet[] = [
    { match: { teamHome: 'Manchester United', teamAway: 'Manchester City', startTime: '2025-02-12T20:00:00Z' }, odds: { home: 2.0, draw: 3.5, away: 1.8 } },
    { match: { teamHome: 'Liverpool', teamAway: 'Everton', startTime: '2025-02-10T19:30:00Z' }, odds: { home: 2.5, draw: 3.8, away: 1.9 } },
    { match: { teamHome: 'Chelsea', teamAway: 'Arsenal', startTime: '2025-02-14T21:00:00Z' }, odds: { home: 1.5, draw: 3.2, away: 2.0 } }
  ];

  constructor(private modalCtrl: ModalController) {}

  async editBet(bet: Bet, event: Event) {
    event.stopPropagation();
    console.log("Bet Clicked:", bet);

    const modal = await this.modalCtrl.create({
      component: EditBetModalComponent,
      componentProps: { bet }
    });

    return await modal.present();
  }

  ngOnInit() {}
}