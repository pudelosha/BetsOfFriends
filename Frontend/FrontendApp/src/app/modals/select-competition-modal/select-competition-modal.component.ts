import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { ModalController } from '@ionic/angular';
import { IonHeader, IonToolbar, IonTitle, IonButtons, IonButton, IonContent, IonItem, IonLabel, IonSelect, IonSelectOption, IonInput } from '@ionic/angular/standalone';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-select-competition-modal',
  templateUrl: './select-competition-modal.component.html',
  styleUrls: ['./select-competition-modal.component.scss'],
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TranslateModule, IonHeader, IonToolbar, IonTitle, IonButtons, IonButton, IonContent, IonItem, IonLabel, IonSelect, IonSelectOption, IonInput]
})
export class SelectCompetitionModalComponent {
  form: FormGroup;

  competitions = [
    { code: 2000, name: 'FIFA World Cup', region: 'World', type: 'International' },
    { code: 2001, name: 'UEFA Champions League', region: 'Europe', type: 'Club' },
    { code: 2002, name: 'Bundesliga', region: 'Germany', type: 'Club' },
    { code: 2003, name: 'Eredivisie', region: 'Netherlands', type: 'Club' },
    { code: 2013, name: 'Campeonato Brasileiro Série A', region: 'Brazil', type: 'Club' },
    { code: 2014, name: 'Primera Division', region: 'Spain', type: 'Club' },
    { code: 2015, name: 'Ligue 1', region: 'France', type: 'Club' },
    { code: 2017, name: 'Primeira Liga', region: 'Portugal', type: 'Club' },
    { code: 2018, name: 'European Championship', region: 'Europe', type: 'International' },
    { code: 2019, name: 'Serie A', region: 'Italy', type: 'Club' },
    { code: 2021, name: 'Premier League', region: 'England', type: 'Club' },
    { code: 2152, name: 'Copa Libertadores', region: 'South America', type: 'Club' },
  ];
  
  constructor(private fb: FormBuilder, private modalCtrl: ModalController) {
    this.form = this.fb.group({
      competitionCode: [2021, [Validators.required, Validators.min(1)]],
      seasonCode: [2024, [Validators.required, Validators.min(2000)]]
    });
  }

  async submit() {
    if (this.form.valid) {
      await this.modalCtrl.dismiss({
        competitionCode: this.form.value.competitionCode,
        seasonCode: this.form.value.seasonCode
      });
    }
  }

  async closeModal() {
    await this.modalCtrl.dismiss(null);
  }
}
