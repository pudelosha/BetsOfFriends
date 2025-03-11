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
    this.actualQualifiedTeam = value.actualQualifiedTeam ?? null;
    this.playerQualifiedTeam = value.playerQualifiedTeam ?? 'Neutral';
  }
  get bet(): Bet {
    return this._bet;
  }

  homeGoals: number = 0;
  awayGoals: number = 0;
  actualQualifiedTeam: 'Home' | 'Away' | null = null;
  playerQualifiedTeam: 'Home' | 'Away' | 'Neutral' | null = 'Neutral'

  constructor(private modalCtrl: ModalController, private toastController: ToastController) {}

  ngAfterViewInit() {}

  saveBet() {
    if (!this._bet) {
      console.error("Error: Bet is undefined!");
      return;
    }
  
    // Log the original bet data before modification
    console.log("Original Bet Data Before Saving:", JSON.parse(JSON.stringify(this._bet)));
  
    // Check if qualification is required (only for ExtendedWithQualification)
    const qualificationRequired = this._bet.type === 'ExtendedWithQualification' &&
                                  this._bet.qualifyHomeOdds !== null && 
                                  this._bet.qualifyAwayOdds !== null;
  
    if (qualificationRequired && this.playerQualifiedTeam === 'Neutral') {
      this.showToast('Please select the team that qualifies.', 'warning');
      return;
    }
  
    // Create object to emit
    const emittedBet = {
      playerHomeGoals: this.homeGoals,
      playerAwayGoals: this.awayGoals,
      playerQualifiedTeam: this.playerQualifiedTeam,
      actualQualifiedTeam: this.actualQualifiedTeam
    };
  
    // Log the emitted bet data
    console.log("Emitting Bet Data:", emittedBet);
  
    this.modalCtrl.dismiss(emittedBet);
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
