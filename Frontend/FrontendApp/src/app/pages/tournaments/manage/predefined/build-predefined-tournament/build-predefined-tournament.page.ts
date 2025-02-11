import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, FormArray, FormControl } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { IonicModule, ToastController } from '@ionic/angular';
import { StageInputTypePage } from '../../stages/stage-input-type/stage-input-type.page';
import { StageTeamsManagementPage } from '../../stages/stage-teams-management/stage-teams-management.page';
import { StageMatchesManagementPage } from '../../stages/stage-matches-management/stage-matches-management.page';
import { StageSummaryPage } from '../../stages/stage-summary/stage-summary.page';
import { buildMatchFormGroup } from '..//..//../shared/form-utils';
import { PredefinedTournamentService } from '../../../../../services/predefined-tournament.service';
import { Router, ActivatedRoute } from '@angular/router';
import { Tournament, Team, Match } from '../../../../../model/tournament-model';
import { EditMatchModalComponent } from 'src/app/modals/edit-match-modal/edit-match-modal.component';
import { EditTeamModalComponent } from 'src/app/modals/edit-team-modal/edit-team-modal.component';
import { ModalController } from '@ionic/angular';


@Component({
  selector: 'app-build-predefined-tournament',
  templateUrl: './build-predefined-tournament.page.html',
  styleUrls: ['./build-predefined-tournament.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, StageInputTypePage, StageTeamsManagementPage, StageMatchesManagementPage, StageSummaryPage],
})
export class BuildPredefinedTournamentPage implements OnInit {
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
      uploadMode: ['append'],
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
        console.error('Invalid or missing tournament id:', id);
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
    if (this.step === 2) {
      const modal = await this.modalController.create({
        component: EditTeamModalComponent,
        componentProps: {
          team: null, // New team
          isEditing: false,
        },
      });
  
      modal.onDidDismiss().then((result) => {
        if (result.data) {
          const newTeam = result.data;
          this.teamsArray.push(
            this.fb.group({
              teamId: [newTeam.teamId], // Can be null for new teams
              teamName: [newTeam.teamName, Validators.required],
            })
          );
          console.log('Added New Team:', newTeam);
        }
      });
  
      await modal.present();
    } else if (this.step === 3) {
      const modal = await this.modalController.create({
        component: EditMatchModalComponent,
        componentProps: {
          match: null, // Passing null to indicate "Add New Match"
          index: undefined, // No existing match to edit
          teams: this.teamsArray.value.map((team: any) => ({
            teamId: team.teamId || null,
            teamName: team.teamName,
          })), // Pass list of teams with both teamId and teamName
        },
      });
  
      modal.onDidDismiss().then((result) => {
        if (result.data) {
          // Add the new match to the matchesArray
          this.matchesArray.push(this.fb.group(result.data));
          console.log('Added New Match:', result.data);
        }
      });
  
      await modal.present();
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
      uploadMode: 'append',
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
    this.teamsArray.clear();
    teams.forEach((team) => {
      this.teamsArray.push(
        this.fb.group({
          teamId: [team.teamId],
          teamName: [team.teamName, Validators.required],
        })
      );
    });
    console.log('Extracted Teams:', this.teamsArray.value);
  }
  
  handleMatchesExtracted(matches: any[]): void {
    this.matchesArray.clear();
    matches.forEach(match => {
      this.matchesArray.push(buildMatchFormGroup(this.fb, match));
    });
    console.log('Extracted Matches:', this.matchesArray.value);
  }
  
  handleTeamsUpdated(updatedTeams: any[]): void {
    this.teamsArray.clear(); // Clear the current array
    updatedTeams.forEach(team => {
      this.teamsArray.push(
        this.fb.group({
          teamName: [team.teamName, Validators.required], // Use teamName field
          teamId: [team.teamId || null], // Include teamId if it exists, otherwise set to null
        })
      );
    });
    console.log('Updated Teams from Child:', this.teamsArray.value);
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
