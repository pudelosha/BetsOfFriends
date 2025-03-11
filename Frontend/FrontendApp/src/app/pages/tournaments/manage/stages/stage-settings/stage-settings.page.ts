import { Component, Input, Output, EventEmitter } from '@angular/core';
import { IonicModule, ToastController, ModalController } from '@ionic/angular';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { TournamentSettings } from 'src/app/model/tournament-model'; // Import the settings interface

@Component({
  selector: 'app-stage-settings',
  templateUrl: './stage-settings.page.html',
  styleUrls: ['./stage-settings.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule],
})
export class StageSettingsPage {
  @Input() settingsData!: TournamentSettings | null; // Accept settings from parent
  @Output() settingsUpdated = new EventEmitter<TournamentSettings>();

  settingsForm: FormGroup;

  exactBonusTypes = [
    { value: 'FixedValue', label: 'Fixed Value' },
    { value: 'OddMultiplied', label: 'Odd Multiplied' }
  ];

  constructor(
    private fb: FormBuilder,
    private modalCtrl: ModalController,
    private toastController: ToastController
  ) {
    this.settingsForm = this.fb.group({
      allowExactResultBonus: [false],
      exactResultBonusCalculation: ['FixedValue'],
      exactResultBonus: [null, Validators.min(1)],

      allowWhoQualifiesBets: [false],

      allowBetsWithBonusAmount: [false],
      maxBetBooster: [1, Validators.min(1)],
      totalBonusAmount: [null, Validators.min(1)],

      allowNonSubmittedBetsPenalty: [false],
      nonSubmittedBetPenalty: [null, Validators.min(1)]
    });
  }

  ngOnInit() {
    if (this.settingsData) {
      this.settingsForm.patchValue(this.settingsData);
    }
  }

  saveSettings() {
    if (this.settingsForm.invalid) {
      this.showToast("Please correct the errors in the form.", "warning");
      return;
    }

    const settings: TournamentSettings = {
      allowExactResultBonus: this.settingsForm.value.allowExactResultBonus ?? false,
      exactResultBonusCalculation: this.settingsForm.value.exactResultBonusCalculation ?? 'FixedValue',
      exactResultBonus: this.settingsForm.value.exactResultBonus ?? null,

      allowWhoQualifiesBets: this.settingsForm.value.allowWhoQualifiesBets ?? false,

      allowBetsWithBonusAmount: this.settingsForm.value.allowBetsWithBonusAmount ?? false,
      maxBetBooster: this.settingsForm.value.maxBetBooster ?? 1,
      totalBonusAmount: this.settingsForm.value.totalBonusAmount ?? null,

      allowNonSubmittedBetsPenalty: this.settingsForm.value.allowNonSubmittedBetsPenalty ?? false,
      nonSubmittedBetPenalty: this.settingsForm.value.nonSubmittedBetPenalty ?? null,
    };

    console.log("Emitting Settings:", settings);
    this.settingsUpdated.emit(settings);
  }

  closeSettings() {
    this.modalCtrl.dismiss(null);
  }

  private async showToast(message: string, color: 'success' | 'warning' | 'danger') {
    const toast = await this.toastController.create({
      message,
      duration: 3000,
      position: 'bottom',
      color,
    });
    await toast.present();
  }
}
