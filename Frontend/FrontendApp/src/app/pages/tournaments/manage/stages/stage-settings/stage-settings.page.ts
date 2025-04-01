import { Component, Input, Output, EventEmitter, OnDestroy } from '@angular/core';
import { IonicModule, ToastController } from '@ionic/angular';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { TournamentSettings } from 'src/app/model/tournament-model';
import { Subscription } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { TranslateModule } from '@ngx-translate/core';


@Component({
  selector: 'app-stage-settings',
  templateUrl: './stage-settings.page.html',
  styleUrls: ['./stage-settings.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule, TranslateModule],
})
export class StageSettingsPage implements OnDestroy {
  @Input() settings!: TournamentSettings;
  @Output() settingsUpdated = new EventEmitter<TournamentSettings>();

  settingsForm: FormGroup;
  private settingsSubscription!: Subscription;

  exactBonusTypes = [
    { value: 'Fixed', label: 'Fixed Value' },
    { value: 'Multiplied', label: 'Odd Multiplied' }
  ];

  constructor(private fb: FormBuilder, private toastController: ToastController) {
    this.settingsForm = this.fb.group({
      allowExactResultBonus: [false],
      exactResultBonusCalculation: ['Fixed'],
      exactResultBonus: [5, [Validators.required, Validators.min(1)]],

      allowWhoQualifiesBets: [false],

      allowBetsWithBooster: [false],
      maxBetBooster: [1, [Validators.required, Validators.min(1)]],
      totalBoosterPool: [100, [Validators.required, Validators.min(1)]],

      allowNonSubmittedBetsPenalty: [false],
      nonSubmittedBetPenalty: [1, [Validators.required]],
    });
  }

  ngOnInit() {
    const mergedSettings: TournamentSettings = {
      allowExactResultBonus: this.settings?.allowExactResultBonus ?? false,
      exactResultBonusCalculation: this.settings?.exactResultBonusCalculation ?? 'Fixed',
      exactResultBonus: this.settings?.exactResultBonus ?? 5,

      allowWhoQualifiesBets: this.settings?.allowWhoQualifiesBets ?? false,

      allowBetsWithBooster: this.settings?.allowBetsWithBooster ?? false,
      maxBetBooster: this.settings?.maxBetBooster ?? 1,
      totalBoosterPool: this.settings?.totalBoosterPool ?? 100,

      allowNonSubmittedBetsPenalty: this.settings?.allowNonSubmittedBetsPenalty ?? false,
      nonSubmittedBetPenalty: this.settings?.nonSubmittedBetPenalty ?? 1,
    };

    console.log("Merging Parent Settings with Defaults:", mergedSettings);
    this.settingsForm.patchValue(mergedSettings, { emitEvent: false });

    // Subscribe to changes, debounce, and prevent redundant emissions
    this.settingsSubscription = this.settingsForm.valueChanges
      .pipe(
        debounceTime(300),
        distinctUntilChanged()
      )
      .subscribe(() => {
        this.handleBoosterValidation();
        this.emitSettings();
      });

    this.emitSettings();
  }

  private handleBoosterValidation() {
    const maxBetBooster = this.settingsForm.get('maxBetBooster')?.value;
    const totalBoosterPool = this.settingsForm.get('totalBoosterPool')?.value;

    if (maxBetBooster > totalBoosterPool) {
      this.settingsForm.get('maxBetBooster')?.setValue(totalBoosterPool, { emitEvent: false });
    }
  }

  private emitSettings() {
    if (!this.settingsForm.valid) {
      console.warn("Settings form is invalid, not emitting data:", this.settingsForm.errors);
      return;
    }

    const settings: TournamentSettings = {
      allowExactResultBonus: this.settingsForm.value.allowExactResultBonus ?? false,
      exactResultBonusCalculation: this.settingsForm.value.exactResultBonusCalculation ?? 'Fixed',
      exactResultBonus: this.settingsForm.value.exactResultBonus ?? null,

      allowWhoQualifiesBets: this.settingsForm.value.allowWhoQualifiesBets ?? false,

      allowBetsWithBooster: this.settingsForm.value.allowBetsWithBooster ?? false,
      maxBetBooster: this.settingsForm.value.maxBetBooster ?? 1,
      totalBoosterPool: this.settingsForm.value.totalBoosterPool ?? null,

      allowNonSubmittedBetsPenalty: this.settingsForm.value.allowNonSubmittedBetsPenalty ?? false,
      nonSubmittedBetPenalty: this.settingsForm.value.nonSubmittedBetPenalty ?? null,
    };

    console.log("Emitting Updated Settings:", settings);
    this.settingsUpdated.emit(settings);
  }

  ngOnDestroy() {
    if (this.settingsSubscription) {
      this.settingsSubscription.unsubscribe();
    }
  }
}
