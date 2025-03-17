import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { IonicModule } from '@ionic/angular';
import { ModalController, AlertController } from '@ionic/angular';
import { EditMatchModalComponent } from 'src/app/modals/edit-match-modal/edit-match-modal.component';
import { buildMatchFormGroup } from '../../../shared/form-utils';
import { Match, Team, Stage } from 'src/app/model/tournament-model';

@Component({
  selector: 'app-stage-matches-management',
  templateUrl: './stage-matches-management.page.html',
  styleUrls: ['./stage-matches-management.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule],
})
export class StageMatchesManagementPage implements OnInit {
  @Input() matchesArray!: FormArray; // FormArray for matches
  @Input() teamsArray!: Team[]; // List of structured teams
  @Input() stagesArray!: Stage[]; // List of structured stages
  @Output() matchesUpdated = new EventEmitter<Match[]>(); // Emits updated matches to parent

  constructor(
    private fb: FormBuilder,
    private modalController: ModalController,
    private alertController: AlertController
  ) {}

  ngOnInit(): void {}

  // Get a specific match control by index
  getMatchControl(index: number): FormGroup {
    return this.matchesArray.at(index) as FormGroup;
  }

  async addMatch(): Promise<void> {
    if (!this.teamsArray || this.teamsArray.length === 0) {
      console.warn('No teams available to add a match.');
      return;
    }

    const modal = await this.modalController.create({
      component: EditMatchModalComponent,
      componentProps: {
        match: null, // Indicate "Add New Match"
        index: undefined, // No existing match to edit
        teams: this.teamsArray, // Pass full team objects
        stages: this.stagesArray, // Pass full stage objects
      },
    });

    modal.onDidDismiss().then((result) => {
      if (result.data) {
        const newMatch: Match = {
          matchFrontendId: this.generateFrontendId(), // Generate frontend ID for new matches
          matchId: null, // New matches have no backend ID initially

          stageId: result.data.stageId ?? null,
          stageFrontendId: result.data.stageFrontendId,
          stageName: result.data.stageName,

          homeTeamId: result.data.homeTeamId ?? null,
          homeTeamFrontendId: result.data.homeTeamFrontendId,
          homeTeam: result.data.homeTeam,

          awayTeamId: result.data.awayTeamId ?? null,
          awayTeamFrontendId: result.data.awayTeamFrontendId,
          awayTeam: result.data.awayTeam,

          matchStart: result.data.matchStart || '',
          matchType: result.data.matchType || 'Regular90Min',
          homeWinOdds: result.data.homeWinOdds ?? 0,
          drawOdds: result.data.drawOdds ?? 0,
          awayWinOdds: result.data.awayWinOdds ?? 0,
          homeQualifies: result.data.homeQualifies ?? null,
          awayQualifies: result.data.awayQualifies ?? null,
        };

        this.matchesArray.push(buildMatchFormGroup(this.fb, newMatch));
        console.log('match before emit');
        console.log(newMatch);
        this.emitMatches();
        console.log('Added New Match:', newMatch);
      }
    });

    await modal.present();
  }

  // Open edit modal for a match
  async openEditModal(index?: number) {
    const existingMatch = index !== undefined ? this.getMatchControl(index).value : null;

    console.log(existingMatch);
    console.log(this.stagesArray);

    const modal = await this.modalController.create({
      component: EditMatchModalComponent,
      componentProps: {
        match: existingMatch || {}, // Pass existing match or an empty object
        index,
        teams: this.teamsArray, // Pass full structured teams
        stages: this.stagesArray, // Pass full stages
      },
    });

    modal.onDidDismiss().then((result) => {
      if (result.data) {
        const updatedMatch: Match = {
          matchFrontendId: result.data.matchFrontendId, // Preserve frontend tracking ID
          matchId: result.data.matchId ?? null,

          stageId: result.data.stageId ?? null,
          stageFrontendId: result.data.stageFrontendId,
          stageName: result.data.stageName,

          homeTeamId: result.data.homeTeamId ?? null,
          homeTeamFrontendId: result.data.homeTeamFrontendId, // Preserve frontend ID
          homeTeam: result.data.homeTeam,

          awayTeamId: result.data.awayTeamId ?? null,
          awayTeamFrontendId: result.data.awayTeamFrontendId, // Preserve frontend ID
          awayTeam: result.data.awayTeam,

          matchStart: result.data.matchStart || '',
          matchType: result.data.matchType || 'Regular90Min',
          homeWinOdds: result.data.homeWinOdds ?? 0,
          drawOdds: result.data.drawOdds ?? 0,
          awayWinOdds: result.data.awayWinOdds ?? 0,
          homeQualifies: result.data.homeQualifies ?? null,
          awayQualifies: result.data.awayQualifies ?? null,
        };

        if (index !== undefined) {
          const matchControl = this.matchesArray.at(index) as FormGroup;
          matchControl.patchValue(updatedMatch); // Use patchValue() to prevent missing fields error
        } else {
          this.matchesArray.push(buildMatchFormGroup(this.fb, updatedMatch));
        }        

        console.log('match before emit ' + updatedMatch)
        this.emitMatches();
        console.log('Updated Match:', updatedMatch);
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
    const updatedMatches: Match[] = this.matchesArray.value.map((match: any) => ({
      matchFrontendId: match.matchFrontendId,
      matchId: match.matchId,

      stageId: match.stageId,
      stageFrontendId: match.stageFrontendId, // Ensure frontendId is preserved
      stageName: match.stageName,

      homeTeamId: match.homeTeamId,
      homeTeamFrontendId: match.homeTeamFrontendId, // Ensure frontendId is preserved
      homeTeam: match.homeTeam,

      awayTeamId: match.awayTeamId,
      awayTeamFrontendId: match.awayTeamFrontendId, // Ensure frontendId is preserved
      awayTeam: match.awayTeam,

      matchStart: match.matchStart,
      matchType: match.matchType,
      homeWinOdds: match.homeWinOdds,
      drawOdds: match.drawOdds,
      awayWinOdds: match.awayWinOdds,
      homeQualifies: match.homeQualifies,
      awayQualifies: match.awayQualifies,
    }));

    this.matchesUpdated.emit(updatedMatches);
    console.log('Emitted Updated Matches:', updatedMatches);
  }

  // Generate unique frontendId for new matches
  private generateFrontendId(): string {
    return 'M-' + Math.random().toString(36).substr(2, 9);
  }
}
