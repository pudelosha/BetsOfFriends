import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ToastController } from '@ionic/angular';
import { ModalController, AlertController } from '@ionic/angular';
import { EditMatchModalComponent } from 'src/app/modals/edit-match-modal/edit-match-modal.component';
import { buildMatchFormGroup } from '../../../shared/form-utils';
import { Match, Team, Stage } from 'src/app/model/tournament-model';
import { TranslateModule } from '@ngx-translate/core';
import { IonList, IonItem, IonButton } from '@ionic/angular/standalone';

@Component({
  selector: 'app-stage-matches-management',
  templateUrl: './stage-matches-management.page.html',
  styleUrls: ['./stage-matches-management.page.scss'],
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TranslateModule, IonList, IonItem, IonButton],
})
export class StageMatchesManagementPage implements OnInit {
  @Input() matchesArray!: FormArray; // FormArray for matches
  @Input() teamsArray!: Team[]; // List of structured teams
  @Input() stagesArray!: Stage[]; // List of structured stages
  @Output() matchesUpdated = new EventEmitter<Match[]>(); // Emits updated matches to parent

  isMobile = false;

  constructor(
    private fb: FormBuilder,
    private modalController: ModalController,
    private alertController: AlertController,
    private toastController: ToastController
  ) {}

  ngOnInit(): void {
    this.checkScreenSize();
    window.addEventListener('resize', this.checkScreenSize.bind(this));
  }

  checkScreenSize(): void {
    this.isMobile = window.innerWidth < 600; // you can tweak the threshold
  }

  getDeleteIcon(status: string): string {
    switch (status) {
      case 'Delete': return 'arrow-undo-outline';
      case 'Uploaded':
      case 'Update':
      case 'New':
      default:
        return 'trash-outline';
    }
  }

  // Get a specific match control by index
  getMatchControl(index: number): FormGroup {
    return this.matchesArray.at(index) as FormGroup;
  }

  async addMatch(): Promise<void> {
    // Filter teams and stages excluding those marked as "Delete"
    const availableTeams = this.teamsArray.filter(t => t.recordStatus !== 'Delete');
    const availableStages = this.stagesArray.filter(s => s.recordStatus !== 'Delete');

    if (!this.teamsArray || this.teamsArray.length === 0) {
      console.warn('No teams available to add a match.');
      return;
    }
  
    const modal = await this.modalController.create({
      component: EditMatchModalComponent,
      componentProps: {
        match: null, // Indicate "Add New Match"
        index: undefined, // No existing match to edit
        teams: availableTeams, // Pass filtered team objects
        stages: availableStages, // Pass filtered stage objects
      },
    });
  
    modal.onDidDismiss().then((result) => {
      if (result.data) {
        const selectedHomeTeam = this.teamsArray.find(t => t.teamFrontendId === result.data.homeTeamFrontendId);
        const selectedAwayTeam = this.teamsArray.find(t => t.teamFrontendId === result.data.awayTeamFrontendId);
        const selectedStage = this.stagesArray.find(s => s.stageFrontendId === result.data.stageFrontendId);
  
        const newMatch: Match = {
          matchFrontendId: this.generateFrontendId(),
          matchId: null,
  
          stageId: selectedStage?.stageId ?? null,
          stageFrontendId: selectedStage?.stageFrontendId ?? '',
          stageName: selectedStage?.stageName ?? '',
  
          homeTeamId: selectedHomeTeam?.teamId ?? null,
          homeTeamFrontendId: selectedHomeTeam?.teamFrontendId ?? '',
          homeTeam: selectedHomeTeam?.teamName ?? '',
  
          awayTeamId: selectedAwayTeam?.teamId ?? null,
          awayTeamFrontendId: selectedAwayTeam?.teamFrontendId ?? '',
          awayTeam: selectedAwayTeam?.teamName ?? '',
  
          matchStart: result.data.matchStart || '',
          matchType: result.data.matchType || 'Regular90Min',
          homeWinOdds: result.data.homeWinOdds ?? 0,
          drawOdds: result.data.drawOdds ?? 0,
          awayWinOdds: result.data.awayWinOdds ?? 0,
          homeQualifies: result.data.homeQualifies ?? null,
          awayQualifies: result.data.awayQualifies ?? null,

          isVisible: result.data.isVisible ?? true,
  
          recordStatus: 'New'
        };
  
        this.matchesArray.push(buildMatchFormGroup(this.fb, newMatch));
        this.emitMatches();
        //console.log('Added New Match:', newMatch);
      }
    });
  
    await modal.present();
  }
  

  // Open edit modal for a match
  async openEditModal(index?: number) {
    const existingMatch = index !== undefined ? this.getMatchControl(index).value : null;

    // Filter out "Deleted" teams and stages
    const availableTeams = this.teamsArray.filter(t => t.recordStatus !== 'Delete');
    const availableStages = this.stagesArray.filter(s => s.recordStatus !== 'Delete');
  
    const modal = await this.modalController.create({
      component: EditMatchModalComponent,
      componentProps: {
        match: existingMatch || {}, // Pass existing match or an empty object
        index,
        teams: availableTeams,
        stages: availableStages,
      },
    });
  
    modal.onDidDismiss().then((result) => {
      if (result.data) {
        const selectedHomeTeam = availableTeams.find(t => t.teamFrontendId === result.data.homeTeamFrontendId);
        const selectedAwayTeam = availableTeams.find(t => t.teamFrontendId === result.data.awayTeamFrontendId);
        const selectedStage = availableStages.find(s => s.stageFrontendId === result.data.stageFrontendId);
    
        const updatedMatch: Match = {
          matchFrontendId: result.data.matchFrontendId,
          matchId: result.data.matchId ?? null,
          externalMatchId: result.data.externalMatchId ?? null,
  
          stageId: selectedStage?.stageId ?? null,
          stageFrontendId: selectedStage?.stageFrontendId ?? '',
          stageName: selectedStage?.stageName ?? '',
  
          homeTeamId: selectedHomeTeam?.teamId ?? null,
          homeTeamFrontendId: selectedHomeTeam?.teamFrontendId ?? '',
          homeTeam: selectedHomeTeam?.teamName ?? '',
  
          awayTeamId: selectedAwayTeam?.teamId ?? null,
          awayTeamFrontendId: selectedAwayTeam?.teamFrontendId ?? '',
          awayTeam: selectedAwayTeam?.teamName ?? '',
  
          matchStart: result.data.matchStart || '',
          matchType: result.data.matchType || 'Regular90Min',
          homeWinOdds: result.data.homeWinOdds ?? 0,
          drawOdds: result.data.drawOdds ?? 0,
          awayWinOdds: result.data.awayWinOdds ?? 0,
          homeQualifies: result.data.homeQualifies ?? null,
          awayQualifies: result.data.awayQualifies ?? null,

          isVisible: result.data.isVisible ?? true,

          matchStatus: result.data.matchStatus ?? null, 
          scoreHome: result.data.scoreHome ?? null,   
          scoreAway: result.data.scoreAway ?? null,   
  
          recordStatus: index !== undefined
            ? (JSON.stringify(existingMatch) !== JSON.stringify(result.data) ? 'Update' : existingMatch.recordStatus)
            : 'New'
        };
  
        if (index !== undefined) {
          const matchControl = this.matchesArray.at(index) as FormGroup;
          matchControl.patchValue(updatedMatch);
        } else {
          this.matchesArray.push(buildMatchFormGroup(this.fb, updatedMatch));
        }
  
        this.emitMatches();
        //console.log('Updated Match:', updatedMatch);
      }
    });
  
    await modal.present();
  }  

  // Remove a match from the FormArray
  async handleRemoveOrUndoMatch(index: number): Promise<void> {
    const matchControl = this.getMatchControl(index);
    const matchToRemove = matchControl.value;
    const currentStatus = matchToRemove.recordStatus;

    if (currentStatus === 'Delete') {
      matchControl.patchValue({ recordStatus: 'Update' });
      this.emitMatches();
      await this.showToast(`Match restored successfully!`, 'success');
    } else {
      const alert = await this.alertController.create({
        header: 'Confirm Removal',
        message: `Are you sure you want to delete the match "${matchToRemove.homeTeam} vs ${matchToRemove.awayTeam}"?`,
        buttons: [
          { text: 'Cancel', role: 'cancel' },
          {
            text: 'Delete',
            role: 'destructive',
            handler: async () => {
              if (currentStatus === 'New') {
                this.matchesArray.removeAt(index);
              } else {
                matchControl.patchValue({ recordStatus: 'Delete' });
              }
              this.emitMatches();
              await this.showToast(`Match removed successfully!`, 'success');
            },
          },
        ],
      });

      await alert.present();
    }
  }

  getDeleteButtonText(recordStatus: string | null): string {
    return recordStatus === 'Delete' ? 'Undo' : 'Delete';
  }

  getDeleteButtonColor(recordStatus: string | null): string {
    return recordStatus === 'Delete' ? 'medium' : 'danger';
  } 

  // Emit updated matches to parent
  private emitMatches(): void {
    const updatedMatches: Match[] = this.matchesArray.value.map((match: any) => ({
      matchFrontendId: match.matchFrontendId,
      matchId: match.matchId,
      externalMatchId: match.externalMatchId ?? null,

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

      isVisible: match.isVisible ?? true,

      matchStatus: match.matchStatus ?? null,  
      scoreHome: match.scoreHome ?? null,     
      scoreAway: match.scoreAway ?? null,    

      recordStatus: match.recordStatus ?? 'Update'
    }));

    this.matchesUpdated.emit(updatedMatches);
    //console.log('Emitted Updated Matches:', updatedMatches);
  }

  getRecordStatusClass(recordStatus: string | null): string {
    switch (recordStatus) {
      case 'New': return 'match-status-new';
      case 'Update': return 'match-status-update';
      case 'Delete': return 'match-status-delete';
      case 'Uploaded': return 'match-status-uploaded';
      case 'Finalised': return 'match-status-finalised';
      default: return '';
    }
  }

  private async showToast(message: string, color: 'success' | 'warning' | 'danger' | 'primary'): Promise<void> {
    const toast = await this.toastController.create({
      message,
      duration: 3000,
      position: 'bottom',
      color,
    });
    await toast.present();
  }

  // Generate unique frontendId for new matches
  private generateFrontendId(): string {
    return 'M-' + Math.random().toString(36).substr(2, 9);
  }
}
