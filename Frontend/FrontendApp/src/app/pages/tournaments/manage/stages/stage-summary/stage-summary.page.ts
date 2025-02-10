import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { IonContent, IonHeader, IonTitle, IonToolbar } from '@ionic/angular/standalone';
import { CommonModule } from '@angular/common';
import { IonicModule, ToastController, AlertController } from '@ionic/angular';
import { FormArray, FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';

@Component({
  selector: 'app-stage-summary',
  templateUrl: './stage-summary.page.html',
  styleUrls: ['./stage-summary.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule]
})
export class StageSummaryPage implements OnInit {
  @Input() tournamentName!: string; // Input for tournament name
  @Input() teamsArray!: string[]; // Input for teams array
  @Input() matchesArray!: any[]; // Input for matches array
  @Output() submitTournament = new EventEmitter<any>(); // EventEmitter for final submission

  constructor() {}

  ngOnInit(): void {
    
  }

  // Emit the finalized tournament data
  submitTournamentData(): void {
    const finalizedTournament = {
      tournamentName: this.tournamentName,
      teams: this.teamsArray,
      matches: this.matchesArray,
    };

    console.log('Finalized Tournament Submitted:', finalizedTournament);
    this.submitTournament.emit(finalizedTournament); // Use emit to send data
  }
}