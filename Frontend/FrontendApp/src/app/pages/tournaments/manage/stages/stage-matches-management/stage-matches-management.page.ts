import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { FormArray, FormBuilder, FormGroup,  ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { IonicModule } from '@ionic/angular';
import { ModalController } from '@ionic/angular';
import { EditMatchModalComponent } from 'src/app/modals/edit-match-modal/edit-match-modal.component';
import { buildMatchFormGroup } from '../../../shared/form-utils';
import { AlertController } from '@ionic/angular';


@Component({
  selector: 'app-stage-matches-management',
  templateUrl: './stage-matches-management.page.html',
  styleUrls: ['./stage-matches-management.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule]
})
export class StageMatchesManagementPage implements OnInit {
  @Input() matchesArray!: FormArray; // FormArray for matches
  @Input() teamsArray!: string[]; // List of teams (optional for modal dropdown)
  @Output() matchesUpdated = new EventEmitter<any[]>(); // Emits updated matches to parent

  constructor(private fb: FormBuilder, private modalController: ModalController, private alertController: AlertController) {}

  ngOnInit(): void {  
  }

  // Get a specific match control by index
  getMatchControl(index: number): FormGroup {
    return this.matchesArray.at(index) as FormGroup;
  }

  // Open edit modal for a match
  async openEditModal(index?: number) {
    const existingMatch = index !== undefined ? this.getMatchControl(index).value : null;
  
    const modal = await this.modalController.create({
      component: EditMatchModalComponent,
      componentProps: {
        match: existingMatch || {}, // Pass existing match or an empty object
        index,
        teams: this.teamsArray, // Pass full list of teams for dropdown
      },
    });
  
    modal.onDidDismiss().then((result) => {
      if (result.data) {
        // Ensure all required fields have values
        const matchData = {
          matchId: result.data.matchId ?? null, // Preserve ID if provided, or set null for new matches
          homeTeamId: result.data.homeTeamId ?? null,
          awayTeamId: result.data.awayTeamId ?? null,
          stage: result.data.stage || null, // Optional, set to null if empty
          homeTeam: result.data.homeTeam || '', // Default to empty string
          awayTeam: result.data.awayTeam || '', // Default to empty string
          matchStart: result.data.matchStart || '', // Default to empty string
          betType: result.data.betType || '90min', // Default to '90min'
          homeWinOdds: result.data.homeWinOdds ?? null,
          drawOdds: result.data.drawOdds ?? null,
          awayWinOdds: result.data.awayWinOdds ?? null,
          homeQualifies: result.data.homeQualifies ?? null,
          awayQualifies: result.data.awayQualifies ?? null,
        };
  
        if (index !== undefined) {
          // Update an existing match
          this.matchesArray.at(index).setValue(matchData);
        } else {
          // Add a new match
          this.matchesArray.push(buildMatchFormGroup(this.fb, matchData));
        }
  
        // Emit matches to the parent
        this.emitMatches();
      }
    });
  
    await modal.present();
  }
      
  // Remove a match from the FormArray
  async removeMatch(index: number): Promise<void> {
    const matchToRemove = this.matchesArray.at(index).value;
  
    // Show confirmation dialog using AlertController
    const alert = await this.alertController.create({
      header: 'Confirm Removal',
      message: `Are you sure you want to delete the match "${matchToRemove.homeTeam} vs ${matchToRemove.awayTeam}"?`,
      buttons: [
        {
          text: 'Cancel',
          role: 'cancel',
        },
        {
          text: 'Delete',
          role: 'destructive',
          handler: () => {
            this.matchesArray.removeAt(index);
            this.emitMatches();
            console.log('Removed match:', matchToRemove);
          },
        },
      ],
    });
  
    await alert.present();
  }

  // Emit updated matches to parent
  private emitMatches(): void {
    const updatedMatches = this.matchesArray.value;
    this.matchesUpdated.emit(updatedMatches); // Emit updated matches
    console.log('Emitted Updated Matches:', updatedMatches);
  }
}