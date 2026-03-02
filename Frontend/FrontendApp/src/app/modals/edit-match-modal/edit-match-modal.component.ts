import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { ModalController, ToastController } from '@ionic/angular';
import {
  IonHeader,
  IonToolbar,
  IonTitle,
  IonButtons,
  IonButton,
  IonSegment,
  IonSegmentButton,
  IonContent,
  IonLabel,
  IonInput,
  IonSelect,
  IonSelectOption,
  IonDatetime,
  IonItem,
  IonToggle
} from '@ionic/angular/standalone';
import { TranslateModule } from '@ngx-translate/core';
import { Team, Stage } from 'src/app/model/tournament-model';

@Component({
  selector: 'app-edit-match-modal',
  templateUrl: './edit-match-modal.component.html',
  styleUrls: ['./edit-match-modal.component.scss'],
  standalone: true,
  imports: [
    IonSegmentButton,
    CommonModule,
    ReactiveFormsModule,
    TranslateModule,
    IonHeader,
    IonSegment,
    IonSegmentButton,
    IonToolbar,
    IonTitle,
    IonButtons,
    IonButton,
    IonContent,
    IonLabel,
    IonInput,
    IonSelect,
    IonSelectOption,
    IonDatetime,
    IonItem,
    IonToggle
  ],
})
export class EditMatchModalComponent implements OnInit {
  @Input() match: any;
  @Input() index?: number;
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
      externalMatchId: [null],

      stageFrontendId: [null, Validators.required],
      stageId: [null],
      stageName: [''],

      homeTeamFrontendId: [null, Validators.required],
      homeTeamId: [null],
      homeTeam: [''],

      awayTeamFrontendId: [null, Validators.required],
      awayTeamId: [null],
      awayTeam: [''],

      matchStart: ['', Validators.required],
      matchType: ['Regular90Min', Validators.required],
      homeWinOdds: [null, Validators.required],
      drawOdds: [null, Validators.required],
      awayWinOdds: [null, Validators.required],
      homeQualifies: [null],
      awayQualifies: [null],

      matchStatus: ['Timed'],
      scoreHome: [null],
      scoreAway: [null],
      qualifiedTeam: ['neutral'],

      isVisible: [true],

      recordStatus: ['New'],
    });

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
        externalMatchId: this.match.externalMatchId || null,
        stageFrontendId: this.match.stageFrontendId || null,
        recordStatus: this.match.recordStatus ?? 'Uploaded',
        scoreHome: this.match?.scoreHome ?? null,
        scoreAway: this.match?.scoreAway ?? null,
        qualifiedTeam: this.match.qualifiedTeam || 'neutral',
        matchStatus: this.match?.matchStatus ?? 'Timed',
        isVisible: this.match.isVisible ?? true,
      });
    } else {
      this.matchForm.patchValue({
        matchFrontendId: this.generateFrontendId(),
        recordStatus: 'New',
        matchStatus: 'Timed',
        isVisible: true
      });
    }

    // Recalculate odds when user changes selected teams in dropdowns
    const homeCtrl = this.matchForm.get('homeTeamFrontendId');
    const awayCtrl = this.matchForm.get('awayTeamFrontendId');

    const recalc = () => this.recalculateOddsFromSelectedTeams();

    homeCtrl?.valueChanges.subscribe(recalc);
    awayCtrl?.valueChanges.subscribe(recalc);

    // Optional: run once for edit mode if both teams are already selected
    this.recalculateOddsFromSelectedTeams();
  }

  compareWith(o1: any, o2: any): boolean {
    return o1 === o2;
  }

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

  // Odds calculator (same logic as in BuildPredefinedTournamentPage)
  private calculateOdds(homeElo: number, awayElo: number) {
    const HOME_ADVANTAGE = 50;

    const round2 = (v: number) => Math.round(v * 100) / 100;
    const clamp = (v: number, min: number, max: number) => Math.max(min, Math.min(max, v));

    // PowerRatio = 1 / (1 + 10^(((AwayElo - HomeElo - 50)/600)))
    const powerRatio =
      1 / (1 + Math.pow(10, (awayElo - homeElo - HOME_ADVANTAGE) / 600));

    // ProbDraw = MAX(0, MIN(0.33, 0.29 - ABS(0.5 - PowerRatio)*0.3))
    const probDraw = clamp(0.29 - Math.abs(0.5 - powerRatio) * 0.3, 0, 0.33);

    // ProbHome = (1 - ProbDraw) * PowerRatio
    const probHome = (1 - probDraw) * powerRatio;

    // ProbAway = 1 - ProbHome - ProbDraw
    const probAway = 1 - probHome - probDraw;

    // Guard
    if (!(probHome > 0) || !(probDraw > 0) || !(probAway > 0)) return null;

    // Odds = ROUND(1/Prob*(0.95 + RAND()/100), 2)
    const jitter = () => 0.95 + Math.random() / 100;

    return {
      homeWinOdds: round2((1 / probHome) * jitter()),
      drawOdds: round2((1 / probDraw) * jitter()),
      awayWinOdds: round2((1 / probAway) * jitter()),
    };
  }

  // Recalculate odds from dropdown-selected teams
  private recalculateOddsFromSelectedTeams(): void {
    const homeId = this.matchForm.value.homeTeamFrontendId;
    const awayId = this.matchForm.value.awayTeamFrontendId;

    if (!homeId || !awayId) return;
    if (homeId === awayId) return;

    // Don't auto-change odds for finished matches
    const matchStatus = (this.matchForm.value.matchStatus ?? '').toLowerCase();
    if (matchStatus === 'finished') return;

    const home = this.teams.find(t => t.teamFrontendId === homeId);
    const away = this.teams.find(t => t.teamFrontendId === awayId);
    if (!home || !away) return;

    const homeElo = Number(home.eloRating ?? 1000);
    const awayElo = Number(away.eloRating ?? 1000);

    console.log('homeId', homeId, 'awayId', awayId);
  console.log('homeTeam', home, 'awayTeam', away);
  console.log('homeElo', homeElo, 'awayElo', awayElo);

    const odds = this.calculateOdds(homeElo, awayElo);
    if (!odds) return;

    this.matchForm.patchValue(
      {
        homeWinOdds: odds.homeWinOdds,
        drawOdds: odds.drawOdds,
        awayWinOdds: odds.awayWinOdds,
      },
      { emitEvent: false }
    );
  }

  async saveMatch() {
    if (this.matchForm.invalid) {
      this.showToast('Please fill in all required fields!', 'danger');
      return;
    }

    if (this.matchForm.value.matchType === 'ExtendedWithQualification') {
      if (this.matchForm.value.homeQualifies === null || this.matchForm.value.awayQualifies === null) {
        this.showToast('Please provide qualification odds for both teams!', 'danger');
        return;
      }
    }

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
      externalMatchId: this.matchForm.value.externalMatchId || null,

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
      homeWinOdds: this.matchForm.value.homeWinOdds ?? null,
      drawOdds: this.matchForm.value.drawOdds ?? null,
      awayWinOdds: this.matchForm.value.awayWinOdds ?? null,
      homeQualifies: this.matchForm.value.homeQualifies ?? null,
      awayQualifies: this.matchForm.value.awayQualifies ?? null,

      isVisible: this.matchForm.value.isVisible ?? true,

      matchStatus: this.matchForm.value.matchStatus || 'Timed',
      scoreHome: this.matchForm.value.scoreHome ?? null,
      scoreAway: this.matchForm.value.scoreAway ?? null,
      qualifiedTeam: this.matchForm.value.qualifiedTeam || null,

      recordStatus: this.index !== undefined
        ? (isUpdated ? 'Update' : this.matchForm.value.recordStatus)
        : 'New'
    };

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

  onHomeTeamChange(ev: any): void {
  const v = ev?.detail?.value ?? null;
  this.matchForm.get('homeTeamFrontendId')?.setValue(v, { emitEvent: true });
  this.recalculateOddsFromSelectedTeams();
}

  onAwayTeamChange(ev: any): void {
    const v = ev?.detail?.value ?? null;
    this.matchForm.get('awayTeamFrontendId')?.setValue(v, { emitEvent: true });
    this.recalculateOddsFromSelectedTeams();
  }
}