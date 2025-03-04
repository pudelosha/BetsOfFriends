import { Component, Input, ViewChild, ElementRef, AfterViewInit } from '@angular/core';
import { Bet } from '../../model/bet';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { IonicModule } from '@ionic/angular';
import { ModalController, ToastController } from '@ionic/angular';


@Component({
  selector: 'app-edit-bet-modal',
  templateUrl: './edit-bet-modal.component.html',
  styleUrls: ['./edit-bet-modal.component.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule, FormsModule],
})
export class EditBetModalComponent implements AfterViewInit {
  @Input() bet!: Bet;

  homeGoals: number = 0;
  awayGoals: number = 0;
  qualifySelection: string = 'neutral';
  goalOptions: number[] = Array.from({ length: 10 }, (_, i) => i); // [0,1,2,3,4,5,6,7,8,9]

  @ViewChild('homePicker') homePicker!: ElementRef;
  @ViewChild('awayPicker') awayPicker!: ElementRef;

  constructor(private modalCtrl: ModalController, private toastController: ToastController) {}

  ngOnInit() {
    if (this.bet) {
      this.homeGoals = this.bet.playerHomeGoals ?? 0;
      this.awayGoals = this.bet.playerAwayGoals ?? 0;
      this.qualifySelection = this.bet.qualifiedTeam === 'Home' ? 'home' 
                         : this.bet.qualifiedTeam === 'Away' ? 'away' 
                         : 'neutral';
    }
  }

  ngAfterViewInit() {
    this.scrollToSelected(this.homePicker.nativeElement, this.homeGoals);
    this.scrollToSelected(this.awayPicker.nativeElement, this.awayGoals);
  }

  scrollToSelected(picker: HTMLElement, value: number) {
    const itemHeight = 50; // Same as in CSS
    picker.scrollTop = value * itemHeight;
  }

  onScroll(event: any, type: 'home' | 'away') {
    const scrollTop = event.target.scrollTop;
    const itemHeight = 50; // Same height as CSS
    const index = Math.round(scrollTop / itemHeight);
    
    if (index >= 0 && index <= 9) { // Ensure we don't go out of range
      if (type === 'home') {
        this.homeGoals = this.goalOptions[index];
      } else {
        this.awayGoals = this.goalOptions[index];
      }
    }
  }

  saveBet() {
    // Check if qualification odds are provided
    const qualificationRequired = this.bet.qualifyHomeOdds !== null && this.bet.qualifyAwayOdds !== null;
  
    // Validate selection if qualification odds are available
    if (qualificationRequired && this.qualifySelection === 'neutral') {
      this.showToast('Please select the team that qualifies.', 'warning');
      return; // Prevent saving if no team is selected
    }
  
    // Determine the qualified team (if applicable)
    const qualifiedTeam =
      this.qualifySelection === 'home' ? 'Home' :
      this.qualifySelection === 'away' ? 'Away' :
      null;
  
    console.log("Bet Saved:", {
      match: this.bet.matchId,
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

  async showToast(message: string, color: 'success' | 'warning' | 'danger') {
    const toast = await this.toastController.create({
      message,
      duration: 3000,
      position: 'bottom',
      color,
    });
    await toast.present();
  }
}
