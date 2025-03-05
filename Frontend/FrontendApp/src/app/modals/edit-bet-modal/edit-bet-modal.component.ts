import { Component, Input, AfterViewInit } from '@angular/core';
import { Bet } from '../../model/bet';
import { CommonModule } from '@angular/common';
import { IonicModule } from '@ionic/angular';
import { ModalController, ToastController } from '@ionic/angular';
import { FormsModule } from '@angular/forms';
import { WheelerNumberPickerComponent } from 'src/app/shared/wheeler-number-picker/wheeler-number-picker.component';

@Component({
  selector: 'app-edit-bet-modal',
  templateUrl: './edit-bet-modal.component.html',
  styleUrls: ['./edit-bet-modal.component.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, FormsModule, WheelerNumberPickerComponent],
})
export class EditBetModalComponent implements AfterViewInit {
  private _bet!: Bet; 

  @Input() set bet(value: Bet) {
    if (!value) {
      console.error("Received undefined or null bet!");
      return;
    }
    this._bet = value;
    this.homeGoals = value.playerHomeGoals ?? 0;
    this.awayGoals = value.playerAwayGoals ?? 0;
    this.qualifySelection = value.qualifiedTeam === 'Home' ? 'home' 
                          : value.qualifiedTeam === 'Away' ? 'away' 
                          : 'neutral';
  }
  get bet(): Bet {
    return this._bet;
  }

  homeGoals: number = 0;
  awayGoals: number = 0;
  qualifySelection: string = 'neutral';

  constructor(private modalCtrl: ModalController, private toastController: ToastController) {}

  ngAfterViewInit() {}

  saveBet() {
    if (!this._bet) {
      console.error("Error: Bet is undefined!");
      return;
    }

    const qualificationRequired = this._bet.qualifyHomeOdds !== null && this._bet.qualifyAwayOdds !== null;
  
    if (qualificationRequired && this.qualifySelection === 'neutral') {
      this.showToast('Please select the team that qualifies.', 'warning');
      return;
    }
  
    const qualifiedTeam =
      this.qualifySelection === 'home' ? 'Home' :
      this.qualifySelection === 'away' ? 'Away' :
      null;
  
    console.log("Bet Saved:", {
      match: this._bet.matchId,
      predictedScore: `${this.homeGoals}-${this.awayGoals}`,
      qualifies: qualifiedTeam
    });
  
    this.modalCtrl.dismiss({
      homeGoals: this.homeGoals,
      awayGoals: this.awayGoals,
      qualifies: qualifiedTeam
    });
  }
  
  closeModal() {
    this.modalCtrl.dismiss();
  }

  private async showToast(message: string, color: 'success' | 'warning' | 'danger') {
    const toast = await this.toastController.create({
      message,
      duration: 3000,
      position: 'bottom',
      color,
    });
    await toast.present();
  }
}
