import { Component, Input, AfterViewInit, CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { ModalController, ToastController } from '@ionic/angular';
import { IonHeader, IonToolbar, IonTitle, IonButtons, IonButton, IonContent, IonGrid, IonRow, IonCol, IonLabel, IonDatetime, IonItem, IonToggle, IonPicker, IonPickerColumn, IonPickerColumnOption, IonSegment, IonSegmentButton } from '@ionic/angular/standalone';
import { TranslateModule } from '@ngx-translate/core';
import { Match } from 'src/app/model/match';

@Component({
  selector: 'app-edit-match-result-modal',
  templateUrl: './edit-match-result-modal.component.html',
  styleUrls: ['./edit-match-result-modal.component.scss'],
  standalone: true,
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
  imports: [CommonModule, ReactiveFormsModule, FormsModule, TranslateModule, IonHeader, IonToolbar, IonTitle, IonButtons, IonButton, IonContent, IonGrid, IonRow, IonCol, IonLabel, IonDatetime, IonItem, IonToggle, IonPicker, IonPickerColumn, IonPickerColumnOption, IonSegment, IonSegmentButton],
})
export class EditMatchResultModalComponent implements AfterViewInit {
  @Input() set match(value: Match) {
    if (!value) {
      console.error("Received undefined or null match!");
      return;
    }
    this._match = value;
    this.matchId = value.matchId;
    this.homeScore = value.homeScore ?? 0;
    this.awayScore = value.awayScore ?? 0;
    this.matchStart = value.matchStart;
    this.matchType = value.matchType;
    this.isFinished = value.isFinished ?? false;
    this.qualifySelection = value.qualifiedTeam === 'Home' ? 'home' 
      : value.qualifiedTeam === 'Away' ? 'away' 
      : 'neutral';
  }
  get match(): Match {
    return this._match;
  }

  private _match!: Match;
  matchId!: number;
  homeScore: number = 0;
  awayScore: number = 0;
  matchStart: string = '';
  matchType: 'Regular90Min' | 'ExtendedWithQualification' = 'Regular90Min';
  isFinished: boolean = false;
  qualifySelection: string = 'neutral';
  goalOptions = Array.from({ length: 11 }, (_, i) => i); // 0 to 10

  constructor(private modalCtrl: ModalController, private toastController: ToastController) {}

  ngAfterViewInit() {}

  onHomeScoreChange(event: CustomEvent) {
    this.homeScore = event.detail.value;
  }
  
  onAwayScoreChange(event: CustomEvent) {
    this.awayScore = event.detail.value;
  }

  saveMatchResult() {
    if (!this._match) {
      console.error("Error: Match is undefined!");
      return;
    }

    if (this.isFinished && this.matchType === 'ExtendedWithQualification' && this.qualifySelection === 'neutral') {
      this.showToast('Please select the team that qualifies.', 'warning');
      return;
    }

    const qualifiedTeam =
      this.qualifySelection === 'home' ? 'Home' :
      this.qualifySelection === 'away' ? 'Away' :
      null;  

    //console.log("Match Result Saved:", {
    //  matchId: this.matchId,
    //  newMatchStart: this.matchStart,
    // finalScore: this.isFinished ? `${this.homeScore}-${this.awayScore}` : 'Match not finished',
    //  qualifies: this.isFinished ? qualifiedTeam : null,
    //  isFinished: this.isFinished
    //});

    this.modalCtrl.dismiss({
      matchId: this.matchId,
      matchStart: this.matchStart,
      homeScore: this.isFinished ? this.homeScore : null,
      awayScore: this.isFinished ? this.awayScore : null,
      qualifiedTeam: this.isFinished ? qualifiedTeam : null,
      isFinished: this.isFinished
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
