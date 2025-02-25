import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, FormArray, FormControl, AbstractControl } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { IonContent, IonHeader, IonTitle, IonToolbar } from '@ionic/angular/standalone';
import { EditMatchModalComponent } from 'src/app/modals/edit-match-modal/edit-match-modal.component';
import { EditTeamModalComponent } from 'src/app/modals/edit-team-modal/edit-team-modal.component';
import { EditUserModalComponent } from 'src/app/modals/edit-user-modal/edit-user-modal.component';
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
  
    // Populate Teams
    this.teamsArray.clear();
    tournament.teams.forEach((team) => {
      this.teamsArray.push(this.fb.group({
        frontendId: [team.frontendId],
        backendId: [team.backendId], 
        teamName: [team.teamName, Validators.required],
      }));
    });
  
    // Populate Matches
    this.matchesArray.clear();
    tournament.matches.forEach((match) => {
      this.matchesArray.push(this.buildMatchFormGroup(match));
    });
  
    // Populate Users
    this.usersArray.clear();
    tournament.users?.forEach((user) => {
      this.usersArray.push(this.fb.group({
        userId: [user.userId],
        userName: [user.userName, Validators.required],
        userAdminName: [user.userAdminName],
        userEmail: [user.userEmail, [Validators.required, Validators.email]],
        status: [user.status, Validators.required],
      }));
    });
  }

  private buildMatchFormGroup(match: Match): FormGroup {
    return this.fb.group({
      frontendId: [match.frontendId],
      backendId: [match.backendId], 
      stage: [match.stage || ''],
      homeTeamId: [match.homeTeamId], 
      homeTeam: [match.homeTeam],
      awayTeamId: [match.awayTeamId],
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
  
    console.log(importMethod);
  
    if (importMethod === 'upload') {
      // Replace all teams
      this.teamsArray.clear();
      teams.forEach((team) => {
        this.teamsArray.push(this.fb.group({
          frontendId: [team.frontendId],
          backendId: [team.backendId], // Null for new teams
          teamName: [team.teamName, Validators.required],
        }));
      });
    } else if (importMethod === 'append') {
      // Append new teams, avoiding duplicates
      teams.forEach((team) => {
        if (!this.teamsArray.value.some((existing: any) => existing.teamName === team.teamName)) {
          this.teamsArray.push(this.fb.group({
            frontendId: [team.frontendId],
            backendId: [team.backendId], 
            teamName: [team.teamName, Validators.required],
          }));
        }
      });
    }
  
    console.log('Updated Teams:', this.teamsArray.value);
  }  
   
  handleMatchesExtracted(matches: Match[]): void {
    const importMethod = this.tournamentForm.get('importMethod')?.value;
  
    console.log(importMethod);
  
    if (importMethod === 'upload') {
      // Replace all matches
      this.matchesArray.clear();
      matches.forEach((match) => {
        this.matchesArray.push(this.buildMatchFormGroup(match));
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
          this.matchesArray.push(this.buildMatchFormGroup(match));
        }
      });
    }
  
    console.log('Updated Matches:', this.matchesArray.value);
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
    //TBA
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
}
