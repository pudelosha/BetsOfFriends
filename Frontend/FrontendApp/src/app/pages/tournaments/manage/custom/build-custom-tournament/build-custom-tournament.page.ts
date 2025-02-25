import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, FormArray, AbstractControl } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { IonicModule, ToastController } from '@ionic/angular';
import { StageInputTypePage } from '../../stages/stage-input-type/stage-input-type.page';
import { StageTeamsManagementPage } from '../../stages/stage-teams-management/stage-teams-management.page';
import { StageMatchesManagementPage } from '../../stages/stage-matches-management/stage-matches-management.page';
import { StageUsersManagementPage } from '../../stages/stage-users-management/stage-users-management.page';
import { StageSummaryPage } from '../../stages/stage-summary/stage-summary.page';
import { Router, ActivatedRoute } from '@angular/router';
import { ModalController } from '@ionic/angular';
import { buildMatchFormGroup } from '../../../shared/form-utils';
import { Tournament } from 'src/app/model/tournament-model';
import { CustomTournamentService } from 'src/app/services/custom-tournament.service';
import { ViewChild } from '@angular/core';
import { Match, Team, User } from 'src/app/model/tournament-model';

@Component({
  selector: 'app-build-custom-tournament',
  templateUrl: './build-custom-tournament.page.html',
  styleUrls: ['./build-custom-tournament.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, StageInputTypePage, StageTeamsManagementPage, StageMatchesManagementPage, StageUsersManagementPage, StageSummaryPage],
})
export class BuildCustomTournamentPage implements OnInit {
  @ViewChild(StageTeamsManagementPage) stageTeamsManagement!: StageTeamsManagementPage;
  @ViewChild(StageMatchesManagementPage) stageMatchesManagement!: StageMatchesManagementPage;
  @ViewChild(StageUsersManagementPage) stageUsersManagement!: StageUsersManagementPage;

  tournamentForm: FormGroup;
  step = 1;
  tournamentId?: number | null = null; // Optional: null for new tournaments, number for existing ones
  isLoading = false;

  constructor(private fb: FormBuilder, 
    private toastController: ToastController,
    private router: Router,
    private route: ActivatedRoute,
    private tournamentService: CustomTournamentService,
    private modalController: ModalController
  ) {
    this.tournamentForm = this.fb.group({
      tournamentId: [null],
      tournamentName: ['', [Validators.required, Validators.maxLength(50)]],
      importMethod: ['upload'],
      teams: this.fb.array([], Validators.required),  // Holds Team models
      matches: this.fb.array([], Validators.required), // Holds Match models
      users: this.fb.array([]), // Holds User models
    });    
  }

  ngOnInit() {
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

  get usersArray(): FormArray {
    return this.tournamentForm.get('users') as FormArray;
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
      case 4:
        if (this.stageUsersManagement) {
          await this.stageUsersManagement.addUser();
        } else {
          console.warn('StageUsersManagement reference is not available.');
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
   
     this.tournamentService.getCustomTournamentById(this.tournamentId).subscribe({
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
   
   populateForm(tournament: Tournament): void {
    this.tournamentForm.patchValue({
      tournamentId: tournament.tournamentId,
      tournamentName: tournament.tournamentName,
      importMethod: 'upload',
    });
  
    // Step 1: Create a lookup map for teams (backendId -> frontendId & name)
    const teamMap = new Map<number, Team>();
  
    this.teamsArray.clear();
    tournament.teams.forEach((team) => {
      if (!team.teamFrontendId) {
        team.teamFrontendId = this.generateFrontendId(); // Ensure frontend ID exists
      }
      
      teamMap.set(team.teamId ?? 0, team); // Map backendId to team object
  
      this.teamsArray.push(
        this.fb.group({
          teamFrontendId: [team.teamFrontendId], // Ensure we store frontendId
          teamId: [team.teamId], // Backend ID
          teamName: [team.teamName, Validators.required],
        })
      );
    });
  
    // Step 2: Populate Matches and assign frontend IDs correctly
    this.matchesArray.clear();
    tournament.matches.forEach((match) => {
      const homeTeam = teamMap.get(match.homeTeamId ?? 0);
      const awayTeam = teamMap.get(match.awayTeamId ?? 0);
  
      this.matchesArray.push(
        this.fb.group({
          matchFrontendId: [match.matchFrontendId || this.generateFrontendId()], // Ensure unique frontendId
          matchId: [match.matchId], // Backend ID
          stage: [match.stage || ''],
  
          homeTeamId: [match.homeTeamId], // Backend ID
          homeTeamFrontendId: [homeTeam?.teamFrontendId || this.generateFrontendId()], // Assign frontend ID
          homeTeam: [homeTeam?.teamName || match.homeTeam], // Ensure correct team name
  
          awayTeamId: [match.awayTeamId], // Backend ID
          awayTeamFrontendId: [awayTeam?.teamFrontendId || this.generateFrontendId()], // Assign frontend ID
          awayTeam: [awayTeam?.teamName || match.awayTeam], // Ensure correct team name
  
          matchStart: [new Date(match.matchStart).toISOString()],
          betType: [match.betType || '90min'],
          homeWinOdds: [match.homeWinOdds],
          drawOdds: [match.drawOdds],
          awayWinOdds: [match.awayWinOdds],
          homeQualifies: [match.homeQualifies],
          awayQualifies: [match.awayQualifies],
        })
      );
    });
  
    console.log('Teams after population:', this.teamsArray.value);
    console.log('Matches after population:', this.matchesArray.value);
  } 

  private buildMatchFormGroup(match: Match): FormGroup {
    return this.fb.group({
      matchFrontendId: [match.matchFrontendId],
      matchId: [match.matchId],
      stage: [match.stage || ''],
      homeTeamId: [match.homeTeamId],
      homeTeamFrontendId: [match.homeTeamFrontendId],
      homeTeam: [match.homeTeam],
      awayTeamId: [match.awayTeamId],
      awayTeamFrontendId: [match.awayTeamFrontendId],
      awayTeam: [match.awayTeam],
      matchStart: [new Date(match.matchStart).toISOString()],
      betType: [match.betType || '90min'],
      homeWinOdds: [match.homeWinOdds],
      drawOdds: [match.drawOdds],
      awayWinOdds: [match.awayWinOdds],
      homeQualifies: [match.homeQualifies],
      awayQualifies: [match.awayQualifies],
    });
  }
         
  handleTeamsExtracted(teams: Team[]): void {
    const importMethod = this.tournamentForm.get('importMethod')?.value;
  
    if (importMethod === 'upload') {
      this.teamsArray.clear();
      teams.forEach((team) => {
        this.teamsArray.push(this.fb.group({
          teamFrontendId: [team.teamFrontendId || this.generateFrontendId()],
          teamId: [team.teamId],
          teamName: [team.teamName, Validators.required],
        }));
      });
    } else if (importMethod === 'append') {
      teams.forEach((team) => {
        if (!this.teamsArray.value.some((existing: any) => existing.teamFrontendId === team.teamFrontendId)) {
          this.teamsArray.push(this.fb.group({
            teamFrontendId: [team.teamFrontendId || this.generateFrontendId()],
            teamId: [team.teamId],
            teamName: [team.teamName, Validators.required],
          }));
        }
      });
    }
  }  
   
  handleMatchesExtracted(matches: Match[]): void {
    const importMethod = this.tournamentForm.get('importMethod')?.value;
  
    if (importMethod === 'upload') {
      this.matchesArray.clear();
      matches.forEach((match) => {
        this.matchesArray.push(this.buildMatchFormGroup(match));
      });
    } else if (importMethod === 'append') {
      matches.forEach((match) => {
        if (
          !this.matchesArray.value.some(
            (existing: any) =>
              existing.matchFrontendId === match.matchFrontendId ||
              (existing.homeTeamFrontendId === match.homeTeamFrontendId &&
                existing.awayTeamFrontendId === match.awayTeamFrontendId &&
                existing.matchStart === match.matchStart)
          )
        ) {
          this.matchesArray.push(this.buildMatchFormGroup(match));
        }
      });
    }
  }  

  handleUsersExtracted(users: User[]): void {
    this.usersArray.clear();
    users.forEach((user) => {
      this.usersArray.push(this.fb.group({
        userId: [user.userId],
        userName: [user.userName, Validators.required],
        userAdminName: [user.userAdminName],
        userEmail: [user.userEmail, [Validators.required, Validators.email]],
        status: [user.status, Validators.required],
      }));
    });
  
    console.log('Updated Users:', this.usersArray.value);
  }
  
  handleUsersUpdated(users: any[]): void {
    
  }
     
  handleTeamsUpdated(teamsData: { previousTeams: Team[]; updatedTeams: Team[] }): void {
    const { previousTeams, updatedTeams } = teamsData;
  
    // Step 1: Create maps for easy lookup using frontendId
    const previousTeamMap = new Map(previousTeams.map(team => [team.teamFrontendId, team]));
    const updatedTeamMap = new Map(updatedTeams.map(team => [team.teamFrontendId, team]));
  
    // Step 2: Detect team name changes based on frontendId (frontendId remains the same)
    const nameUpdates = updatedTeams.filter(updatedTeam => {
      const previousTeam = previousTeamMap.get(updatedTeam.teamFrontendId);
      return previousTeam && previousTeam.teamName !== updatedTeam.teamName;
    });
  
    // Step 3: Update matches where a team name has changed (retain frontendId)
    if (nameUpdates.length > 0) {
      nameUpdates.forEach(updatedTeam => {
        this.matchesArray.controls.forEach((control: AbstractControl) => {
          const match = (control as FormGroup).value;
  
          if (match.homeTeamFrontendId === updatedTeam.teamFrontendId) {
            (control as FormGroup).patchValue({ homeTeam: updatedTeam.teamName });
          }
          if (match.awayTeamFrontendId === updatedTeam.teamFrontendId) {
            (control as FormGroup).patchValue({ awayTeam: updatedTeam.teamName });
          }
        });
      });
    }
  
    // Step 4: Ensure matches are only removed if a team is truly deleted
    const updatedTeamFrontendIds = new Set(updatedTeams.map(team => team.teamFrontendId));
  
    // Do NOT remove matches where team names are updated; only remove matches if the team is gone
    const remainingMatches = this.matchesArray.controls.filter((control: AbstractControl) => {
      const match = (control as FormGroup).value;
      return updatedTeamFrontendIds.has(match.homeTeamFrontendId) && updatedTeamFrontendIds.has(match.awayTeamFrontendId);
    });
  
    // Step 5: Preserve matches, do not erase existing ones
    this.matchesArray.clear();
    remainingMatches.forEach((control: AbstractControl) => {
      this.matchesArray.push(this.fb.group(control.value));
    });
  
    console.log('Updated Matches after team update:', this.matchesArray.value);
  }  
    
  handleMatchesUpdated(updatedMatches: Match[]): void {
    this.matchesArray.clear();
    updatedMatches.forEach(match => {
      this.matchesArray.push(this.buildMatchFormGroup(match));
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
  
    if (this.usersArray.length < 1) {
      this.showToast('At least 1 user is required!', 'danger');
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
        teamId: isEditing ? team.teamId || null : null, // Use `null` for new teams
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
      users: this.usersArray.value.map((user: any) => ({
        userId: user.userId || null, // Null for new users
        userName: user.userName,
        userAdminName: user.userAdminName,
        userEmail: user.userEmail,
        status: user.status,
      })),
    };
  
    console.log('Finalized Custom Tournament Data:', tournamentData);
  
    const submitObservable = isEditing
      ? this.tournamentService.updateCustomTournament(tournamentData)
      : this.tournamentService.createCustomTournament(tournamentData);
  
    submitObservable.subscribe({
      next: (response) => {
        console.log('Server response:', response); // Log the plain text response
        this.router.navigate(['/tournaments/custom']).then(() => {
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
    if (canProceed && this.step < 5) {
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
        if (this.usersArray.length === 0) {
          await this.showToast('At least 1 user is required!', 'danger');
          return false;
        }
        return true;
  
      case 5:
        return true; // No validation needed for the summary
    }
    return false; // Default fallback
  }  

  private generateFrontendId(): string {
    return 'T-' + Math.random().toString(36).substr(2, 9);
  }
}
