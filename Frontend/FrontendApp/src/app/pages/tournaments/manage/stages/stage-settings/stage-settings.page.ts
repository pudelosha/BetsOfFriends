import { Component, Input, Output, EventEmitter, OnDestroy } from '@angular/core';
import { IonicModule, ToastController } from '@ionic/angular';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { TournamentSettings } from 'src/app/model/tournament-model';
import { Subscription } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';

@Component({
  selector: 'app-stage-settings',
  templateUrl: './stage-settings.page.html',
  styleUrls: ['./stage-settings.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule],
})
export class StageSettingsPage implements OnDestroy {
  @Input() settings!: TournamentSettings;
  @Output() settingsUpdated = new EventEmitter<TournamentSettings>();

  settingsForm: FormGroup;
  private settingsSubscription!: Subscription;

  exactBonusTypes = [
    { value: 'FixedValue', label: 'Fixed Value' },
    { value: 'OddMultiplied', label: 'Odd Multiplied' }
  ];

  constructor(private fb: FormBuilder, private toastController: ToastController) {
    this.settingsForm = this.fb.group({
      allowExactResultBonus: [false],
      exactResultBonusCalculation: ['FixedValue'],
      exactResultBonus: [5, [Validators.required, Validators.min(1)]],

      allowWhoQualifiesBets: [false],

      allowBetsWithBonusAmount: [false],
      maxBetBooster: [1, [Validators.required, Validators.min(1)]],
      totalBonusAmount: [100, [Validators.required, Validators.min(1)]],

      allowNonSubmittedBetsPenalty: [false],
      nonSubmittedBetPenalty: [-1, [Validators.required]],
    });
  }

  ngOnInit() {
    // **Ensure defaults are used if input settings contain `null` or `undefined`**
    const mergedSettings: TournamentSettings = {
      allowExactResultBonus: this.settings?.allowExactResultBonus ?? false,
      exactResultBonusCalculation: this.settings?.exactResultBonusCalculation ?? 'FixedValue',
      exactResultBonus: this.settings?.exactResultBonus ?? 5,

      allowWhoQualifiesBets: this.settings?.allowWhoQualifiesBets ?? false,

      allowBetsWithBonusAmount: this.settings?.allowBetsWithBonusAmount ?? false,
      maxBetBooster: this.settings?.maxBetBooster ?? 1,
      totalBonusAmount: this.settings?.totalBonusAmount ?? 100,

      allowNonSubmittedBetsPenalty: this.settings?.allowNonSubmittedBetsPenalty ?? false,
      nonSubmittedBetPenalty: this.settings?.nonSubmittedBetPenalty ?? -1,
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
        this.emitSettings();
      });

    // Emit initial settings only once
    this.emitSettings();
  }

  private emitSettings() {
    if (!this.settingsForm.valid) {
      console.warn("Form Invalid, Not Emitting");
      return;
    }

    const settings: TournamentSettings = {
      allowExactResultBonus: this.settingsForm.value.allowExactResultBonus,
      exactResultBonusCalculation: this.settingsForm.value.exactResultBonusCalculation,
      exactResultBonus: this.settingsForm.value.exactResultBonus,

      allowWhoQualifiesBets: this.settingsForm.value.allowWhoQualifiesBets,

      allowBetsWithBonusAmount: this.settingsForm.value.allowBetsWithBonusAmount,
      maxBetBooster: this.settingsForm.value.maxBetBooster,
      totalBonusAmount: this.settingsForm.value.totalBonusAmount,

      allowNonSubmittedBetsPenalty: this.settingsForm.value.allowNonSubmittedBetsPenalty,
      nonSubmittedBetPenalty: this.settingsForm.value.nonSubmittedBetPenalty,
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
