import { Component, Input, OnInit } from '@angular/core';
import { IonicModule, ModalController, ToastController } from '@ionic/angular';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { Team, Stage } from 'src/app/model/tournament-model';
import { TranslateModule } from '@ngx-translate/core';


@Component({
  selector: 'app-edit-match-modal',
  templateUrl: './edit-match-modal.component.html',
  styleUrls: ['./edit-match-modal.component.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule, TranslateModule],
})
export class EditMatchModalComponent implements OnInit {
  @Input() match: any; // Existing match (if editing), otherwise null
  @Input() index?: number; // Index in the match array
  @Input() teams: Team[] = [];
  @Input() stages: Stage[] = [];

  matchForm: FormGroup;

  constructor(
    private modalController: ModalController,
    private fb: FormBuilder,
    private toastController: ToastController
  ) {
    this.matchForm = this.fb.group({
      matchFrontendId: [null],  
      matchId: [null],    

      stageFrontendId: [null, Validators.required], // Use frontendId for validation
      stageId: [null],
      stageName: [''],
    
      homeTeamFrontendId: [null, Validators.required], // Use frontendId for validation
      homeTeamId: [null],  
      homeTeam: [''],  
    
      awayTeamFrontendId: [null, Validators.required], // Use frontendId for validation
      awayTeamId: [null],  
      awayTeam: [''], 

      matchStart: ['', Validators.required],  
      matchType: ['Regular90Min', Validators.required],  
      homeWinOdds: [null, Validators.required],  
      drawOdds: [null, Validators.required],  
      awayWinOdds: [null, Validators.required],  
      homeQualifies: [null],  
      awayQualifies: [null],  

      isVisible: [true],

      recordStatus: ['New'], // Default to "New"
    });

    // Listen for matchType changes
    this.matchForm.get('matchType')?.valueChanges.subscribe((value) => {
      this.toggleQualificationOddsValidation(value);
    });
  }

  ngOnInit() {
    if (this.match) {
      this.matchForm.patchValue({
        ...this.match,
        matchFrontendId: this.match.matchFrontendId || this.generateFrontendId(),
        homeTeamFrontendId: this.match.homeTeamFrontendId || null,
        awayTeamFrontendId: this.match.awayTeamFrontendId || null,
        stageFrontendId: this.match.stageFrontendId || null,
        recordStatus: this.match.recordStatus ?? 'Uploaded',
        isVisible: this.match.isVisible ?? true,
      });
    } else {
      this.matchForm.patchValue({
        matchFrontendId: this.generateFrontendId(),
        recordStatus: 'New',
        isVisible: true
      });
    }
  }  

  // Ensure `ion-select` properly binds values
  compareWith(o1: any, o2: any): boolean {
    return o1 === o2;
  }

  // Function to dynamically apply validation
  private toggleQualificationOddsValidation(matchType: string) {
    const homeQualifiesControl = this.matchForm.get('homeQualifies');
    const awayQualifiesControl = this.matchForm.get('awayQualifies');

    if (matchType === 'ExtendedWithQualification') {
      homeQualifiesControl?.setValidators([Validators.required]);
      awayQualifiesControl?.setValidators([Validators.required]);
    } else {
      homeQualifiesControl?.clearValidators();
      awayQualifiesControl?.clearValidators();
      homeQualifiesControl?.setValue(null);
      awayQualifiesControl?.setValue(null);
    }

    homeQualifiesControl?.updateValueAndValidity();
    awayQualifiesControl?.updateValueAndValidity();
  }

  async saveMatch() {
    if (this.matchForm.invalid) {
      this.showToast('Please fill in all required fields!', 'danger');
      console.log(this.matchForm);
      return;
    }

    if (this.matchForm.value.matchType === 'ExtendedWithQualification') {
      if (this.matchForm.value.homeQualifies === null || this.matchForm.value.awayQualifies === null) {
        this.showToast('Please provide qualification odds for both teams!', 'danger');
        return;
      }
    }

    // Find selected team objects based on frontendId
    const selectedStage = this.stages.find(s => s.stageFrontendId === this.matchForm.value.stageFrontendId);
    const selectedHomeTeam = this.teams.find(t => t.teamFrontendId === this.matchForm.value.homeTeamFrontendId);
    const selectedAwayTeam = this.teams.find(t => t.teamFrontendId === this.matchForm.value.awayTeamFrontendId);  

    if (!selectedHomeTeam || !selectedAwayTeam) {
      this.showToast('Invalid team selection!', 'danger');
      return;
    }

    if (!selectedStage) {
      this.showToast('Invalid stage selection!', 'danger');
      return;
    }

    const isUpdated = this.match &&
      (this.match.stageFrontendId !== selectedStage.stageFrontendId ||
      this.match.homeTeamFrontendId !== selectedHomeTeam.teamFrontendId ||
      this.match.awayTeamFrontendId !== selectedAwayTeam.teamFrontendId ||
      this.match.matchStart !== this.matchForm.value.matchStart ||
      this.match.matchType !== this.matchForm.value.matchType ||
      this.match.homeWinOdds !== this.matchForm.value.homeWinOdds ||
      this.match.drawOdds !== this.matchForm.value.drawOdds ||
      this.match.awayWinOdds !== this.matchForm.value.awayWinOdds ||
      this.match.homeQualifies !== this.matchForm.value.homeQualifies ||
      this.match.awayQualifies !== this.matchForm.value.awayQualifies);

    const matchData = {
      matchFrontendId: this.matchForm.value.matchFrontendId,
      matchId: this.match?.matchId || null,

      stageFrontendId: selectedStage.stageFrontendId,
      stageId: selectedStage.stageId || null,
      stageName: selectedStage.stageName,
  
      homeTeamFrontendId: selectedHomeTeam.teamFrontendId,
      homeTeamId: selectedHomeTeam.teamId || null,
      homeTeam: selectedHomeTeam.teamName,
  
      awayTeamFrontendId: selectedAwayTeam.teamFrontendId,
      awayTeamId: selectedAwayTeam.teamId || null,
      awayTeam: selectedAwayTeam.teamName,

      matchStart: this.matchForm.value.matchStart || '',
      matchType: this.matchForm.value.matchType || 'Regular90Min',
      homeWinOdds: this.matchForm.value.homeWinOdds || null,
      drawOdds: this.matchForm.value.drawOdds || null,
      awayWinOdds: this.matchForm.value.awayWinOdds || null,
      homeQualifies: this.matchForm.value.homeQualifies || null,
      awayQualifies: this.matchForm.value.awayQualifies || null,

      isVisible: this.matchForm.value.isVisible ?? true,

      recordStatus: this.index !== undefined
        ? (isUpdated ? 'Update' : this.matchForm.value.recordStatus) 
        : 'New'
    };

    console.log('Saving Match:', matchData);

    await this.modalController.dismiss(matchData);
    this.showToast(this.index !== undefined ? 'Match updated!' : 'New match added!', 'success');
  }

  closeModal() {
    this.modalController.dismiss(null);
  }

  async showToast(message: string, color: 'success' | 'danger') {
    const toast = await this.toastController.create({
      message,
      duration: 3000,
      position: 'bottom',
      color,
    });
    await toast.present();
  }

  private generateFrontendId(): string {
    return 'M-' + Math.random().toString(36).substr(2, 9);
  }
}
