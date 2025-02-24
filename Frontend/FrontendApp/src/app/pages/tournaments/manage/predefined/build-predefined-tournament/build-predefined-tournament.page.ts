import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, FormArray, FormControl, AbstractControl } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { IonicModule, ToastController } from '@ionic/angular';
import { StageInputTypePage } from '../../stages/stage-input-type/stage-input-type.page';
import { StageTeamsManagementPage } from '../../stages/stage-teams-management/stage-teams-management.page';
import { StageMatchesManagementPage } from '../../stages/stage-matches-management/stage-matches-management.page';
import { StageSummaryPage } from '../../stages/stage-summary/stage-summary.page';
import { buildMatchFormGroup } from '../../../shared/form-utils';
import { PredefinedTournamentService } from 'src/app/services/predefined-tournament.service';
import { Router, ActivatedRoute } from '@angular/router';
import { Tournament, Team, Match } from '../../../../../model/tournament-model';
import { EditMatchModalComponent } from 'src/app/modals/edit-match-modal/edit-match-modal.component';
import { EditTeamModalComponent } from 'src/app/modals/edit-team-modal/edit-team-modal.component';
import { ModalController } from '@ionic/angular';
import { ViewChild } from '@angular/core';

@Component({
  selector: 'app-build-predefined-tournament',
  templateUrl: './build-predefined-tournament.page.html',
  styleUrls: ['./build-predefined-tournament.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, StageInputTypePage, StageTeamsManagementPage, StageMatchesManagementPage, StageSummaryPage],
})
export class BuildPredefinedTournamentPage implements OnInit {
  @ViewChild(StageTeamsManagementPage) stageTeamsManagement!: StageTeamsManagementPage;
  @ViewChild(StageMatchesManagementPage) stageMatchesManagement!: StageMatchesManagementPage;

  tournamentForm: FormGroup;
  step = 1;
  tournamentId?: number | null = null; // Optional: null for new tournaments, number for existing ones
  isLoading = false;

  constructor(private fb: FormBuilder, 
    private toastController: ToastController,
    private router: Router,
    private route: ActivatedRoute,
    private tournamentService: PredefinedTournamentService,
    private modalController: ModalController
  ) {
    this.tournamentForm = this.fb.group({
      tournamentId: [null],
      tournamentName: ['', [Validators.required, Validators.maxLength(50)]],
      importMethod: ['upload'],
      teams: this.fb.array([], Validators.required),
      matches: this.fb.array([]),
    });
  }

  ngOnInit(): void {
    this.route.paramMap.subscribe((params) => {
      const id = params.get('id');
      if (id && !isNaN(+id)) {
        this.tournamentId = +id; // Convert the id to a number
        this.loadTournament();
      } else {
        this.tournamentId = null;
      }
    });
  }

  ionViewWillEnter(): void {
    this.resetFormData();
    this.scrollToTop();
    this.step = 1;
  }

  private resetFormData(): void {
    this.tournamentForm.reset();
    this.teamsArray.clear();
    this.matchesArray.clear();
    this.tournamentForm.get('importMethod')?.setValue('upload');
  }

  private scrollToTop(): void {
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  get teamsArray(): FormArray {
    return this.tournamentForm.get('teams') as FormArray;
  }

  get matchesArray(): FormArray {
    return this.tournamentForm.get('matches') as FormArray;
  }

  async openAddModal(): Promise<void> {
    switch (this.step) {
      case 2:
        if (this.stageTeamsManagement) {
          await this.stageTeamsManagement.addTeam();
        } else {
          console.warn('StageTeamsManagementPage reference is not available.');
        }
        break;
      case 3:
        if (this.stageMatchesManagement) {
          await this.stageMatchesManagement.addMatch();
        } else {
          console.warn('StageMatchesManagement reference is not available.');
        }
        break;
      default:
        console.warn('Invalid step for adding data:', this.step);
    }
  }

  private loadTournament(): void {
    if (!this.tournamentId) {
      console.error('Tournament ID is missing.');
      return;
    }
  
    this.tournamentService.getPredefinedTournamentById(this.tournamentId).subscribe({
      next: (tournament) => {
        if (tournament) {
          this.populateForm(tournament);
        } else {
          console.error('Tournament not found:', this.tournamentId);
        }
      },
      error: (err) => {
        console.error('Error loading tournament:', err);
      },
    });
  }  
  
  private populateForm(tournament: Tournament): void {
    this.tournamentForm.patchValue({
      tournamentId: tournament.tournamentId,
      tournamentName: tournament.tournamentName,
      importMethod: 'upload',
    });
  
    // Populate teams
    this.teamsArray.clear();
    tournament.teams.forEach((team) => {
      this.teamsArray.push(
        this.fb.group({
          teamId: [team.teamId],
          teamName: [team.teamName, Validators.required],
        })
      );
    });
  
    // Populate matches
    this.matchesArray.clear();
    tournament.matches.forEach((match) => {
      this.matchesArray.push(
        this.fb.group({
          matchId: match.matchId,
          stage: match.stage,
          homeTeamId: match.homeTeamId,
          homeTeam: match.homeTeam,
          awayTeamId: match.awayTeamId,
          awayTeam: match.awayTeam,
          matchStart: match.matchStart,
          betType: match.betType,
          homeWinOdds: match.homeWinOdds,
          drawOdds: match.drawOdds,
          awayWinOdds: match.awayWinOdds,
          homeQualifies: match.homeQualifies,
          awayQualifies: match.awayQualifies,
        })
      );
    });
  }
      
  handleTeamsExtracted(teams: { teamId: number | null; teamName: string }[]): void {
    const importMethod = this.tournamentForm.get('importMethod')?.value;

    console.log(importMethod);

    if (importMethod === 'upload') {
      // Clear and replace all teams
      this.teamsArray.clear();
      teams.forEach((team) => {
        this.teamsArray.push(
          this.fb.group({
            teamId: [team.teamId],
            teamName: [team.teamName, Validators.required],
          })
        );
      });
    } else if (importMethod === 'append') {
      // Append new teams, avoiding duplicates
      teams.forEach((team) => {
        if (!this.teamsArray.value.some((existing: any) => existing.teamName === team.teamName)) {
          this.teamsArray.push(
            this.fb.group({
              teamId: [team.teamId],
              teamName: [team.teamName, Validators.required],
            })
          );
        }
      });
    }
  
    console.log('Updated Teams:', this.teamsArray.value);
  }
  
  handleMatchesExtracted(matches: any[]): void {
    const importMethod = this.tournamentForm.get('importMethod')?.value;

    console.log(importMethod);
  
    if (importMethod === 'upload') {
      // Clear and replace all matches
      this.matchesArray.clear();
      matches.forEach((match) => {
        this.matchesArray.push(buildMatchFormGroup(this.fb, match));
      });
    } else if (importMethod === 'append') {
      // Append new matches, avoiding duplicates
      matches.forEach((match) => {
        if (
          !this.matchesArray.value.some(
            (existing: any) =>
              existing.homeTeam === match.homeTeam &&
              existing.awayTeam === match.awayTeam &&
              existing.matchStart === match.matchStart
          )
        ) {
          this.matchesArray.push(buildMatchFormGroup(this.fb, match));
        }
      });
    }
  
    console.log('Updated Matches:', this.matchesArray.value);
  }  
    
  handleTeamsUpdated(teamsData: { previousTeams: any[]; updatedTeams: any[] }): void {
    const { previousTeams, updatedTeams } = teamsData;
    
    // Step 1: Create maps of teamId to teamName for previous and updated states
    const previousTeamMap = previousTeams.reduce((map: any, team: any) => {
      map[team.teamId] = team.teamName;
      return map;
    }, {});
  
    const updatedTeamMap = updatedTeams.reduce((map: any, team: any) => {
      map[team.teamId] = team.teamName;
      return map;
    }, {});
   
    // Step 2: Detect team name changes
    const nameUpdates = updatedTeams.filter((updatedTeam: any) => {
      const previousTeamName = previousTeamMap[updatedTeam.teamId];
      return previousTeamName && previousTeamName !== updatedTeam.teamName;
    });
    
    // Step 3: Update matchesArray for team name changes
    if (nameUpdates.length > 0) {
      nameUpdates.forEach((updatedTeam: any) => {
        const previousTeamName = previousTeamMap[updatedTeam.teamId];
  
        this.matchesArray.controls.forEach((control: AbstractControl) => {
          const match = (control as FormGroup).value;
  
          if (match.homeTeam === previousTeamName) {
            (control as FormGroup).patchValue({ homeTeam: updatedTeam.teamName });
          }
          if (match.awayTeam === previousTeamName) {
            (control as FormGroup).patchValue({ awayTeam: updatedTeam.teamName });
          }
        });
      });
    }
  
    // Step 4: Remove matches where home or away teams no longer exist
    const updatedTeamNames = updatedTeams.map((team: any) => team.teamName);
    const filteredMatches = this.matchesArray.controls.filter((control: AbstractControl) => {
      const match = (control as FormGroup).value;
      const isValidMatch = updatedTeamNames.includes(match.homeTeam) && updatedTeamNames.includes(match.awayTeam);
      if (!isValidMatch) {
      }
      return isValidMatch;
    });
  
    // Step 5: Clear and rebuild matchesArray with filtered and updated matches
    this.matchesArray.clear();
    filteredMatches.forEach((control: AbstractControl) => {
      const match = (control as FormGroup).value;
      this.matchesArray.push(this.fb.group(match));
    });
  
    // Step 6: Emit updated matches
    const finalUpdatedMatches = this.matchesArray.controls.map((control: AbstractControl) =>
      (control as FormGroup).value
    );
  }  
   
  handleMatchesUpdated(updatedMatches: any[]): void {
    this.matchesArray.clear();
    updatedMatches.forEach(match => {
      this.matchesArray.push(buildMatchFormGroup(this.fb, match));
    });
    console.log('Updated Matches from Child:', this.matchesArray.value);
  }
  
  submitTournament(): void {
    // Validate Tournament Data
    if (!this.tournamentForm.value.tournamentName?.trim()) {
      this.showToast('Tournament name is required!', 'danger');
      return;
    }
  
    if (this.teamsArray.length < 2) {
      this.showToast('At least 2 teams are required to create a tournament!', 'danger');
      return;
    }
  
    if (this.matchesArray.length < 1) {
      this.showToast('At least 1 match is required!', 'danger');
      return;
    }
  
    // Show the spinner
    this.isLoading = true;
  
    // Prepare Tournament Data
    const isEditing = !!this.tournamentId;
  
    const tournamentData: Tournament = {
      tournamentId: isEditing ? this.tournamentId : null,
      tournamentName: this.tournamentForm.value.tournamentName,
      isActive: true,
      createdBy: this.tournamentForm.value.createdBy || 'Admin',
      createdAt: this.tournamentForm.value.createdAt || new Date().toISOString(),
      teams: this.teamsArray.value.map((team: { teamId: number | null; teamName: string }) => ({
        teamId: team.teamId || null, // Use `null` for new teams
        teamName: team.teamName,
      })),
      matches: this.matchesArray.value.map((match: any) => ({
        matchId: isEditing ? match.matchId || null : null,
        stage: match.stage || '',
        homeTeamId: isEditing ? match.homeTeamId || null : null,
        awayTeamId: isEditing ? match.awayTeamId || null : null,
        homeTeam: match.homeTeam,
        awayTeam: match.awayTeam,
        betType: match.betType || '90min',
        matchStart: new Date(match.matchStart).toISOString(),
        homeWinOdds: match.homeWinOdds,
        drawOdds: match.drawOdds,
        awayWinOdds: match.awayWinOdds,
        homeQualifies: match.homeQualifies,
        awayQualifies: match.awayQualifies,
      })),
    };
  
    console.log('Finalized Tournament Data:', tournamentData);
  
    const submitObservable = isEditing
      ? this.tournamentService.updatePredefinedTournament(tournamentData)
      : this.tournamentService.createPredefinedTournament(tournamentData);
  
    submitObservable.subscribe({
      next: (response) => {
        console.log('Server response:', response); // Log the plain text response
        this.router.navigate(['/tournaments/predefined']).then(() => {
          this.showToast('Tournament updated successfully!', 'success');
          this.isLoading = false; // Hide the spinner
        });
      },
      error: (error) => {
        console.error('Error submitting tournament:', error);
        this.showToast('Error submitting tournament!', 'danger');
        this.isLoading = false; // Hide the spinner even on error
      },
    });
  }
         
  async nextStep(): Promise<void> {
    const canProceed = await this.canProceed();
    if (canProceed && this.step < 4) {
      this.scrollToTop();
      this.step++;
    }
  }
  
  prevStep(): void {
    if (this.step > 1) {
      this.scrollToTop();
      this.step--;
    }
  }

  async showToast(message: string, color: 'success' | 'warning' | 'danger' | 'primary'): Promise<void> {
    const toast = await this.toastController.create({
      message,
      duration: 3000,
      position: 'bottom',
      color,
    });
    await toast.present();
  }
  
  async canProceed(): Promise<boolean> {
    switch (this.step) {
      case 1:
        if (!this.tournamentForm.get('tournamentName')?.valid) {
          await this.showToast('Tournament Name is required!', 'danger');
          return false;
        }
        return true;
  
      case 2:
        if (this.teamsArray.length <= 1) {
          await this.showToast('At least 2 teams are required!', 'danger');
          return false;
        }
        return true;
  
      case 3:
        if (this.matchesArray.length === 0) {
          await this.showToast('At least 1 match is required!', 'danger');
          return false;
        }
        return true;
  
      case 4:
        return true; // No validation needed for the summary
    }
    return false; // Default fallback
  }
}
