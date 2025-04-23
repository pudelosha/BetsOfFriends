import { Component, Input } from '@angular/core';
import { ModalController } from '@ionic/angular';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { IonContent, IonButton, IonSearchbar, IonItem, IonList, IonHeader, IonToolbar, IonTitle, IonButtons } from '@ionic/angular/standalone';

@Component({
  selector: 'app-select-country-modal',
  templateUrl: './select-country-modal.component.html',
  standalone: true,
  imports: [IonContent, IonButton, IonButtons, IonTitle, IonToolbar, IonHeader, CommonModule, FormsModule, IonSearchbar, IonList, IonItem],
})
export class SelectCountryModalComponent {
  @Input() countries: { countryId: number; name: string }[] = [];

  searchText = '';
  filteredCountries: { countryId: number; name: string }[] = [];

  constructor(private modalCtrl: ModalController) {}

  ngOnInit() {
    this.filteredCountries = [...this.countries];
  }

  filterCountries() {
    const text = this.searchText.toLowerCase();
    this.filteredCountries = this.countries.filter(c =>
      c.name.toLowerCase().includes(text)
    );
  }

  selectCountry(countryId: number) {
    this.modalCtrl.dismiss(countryId);
  }

  closeModal() {
    this.modalCtrl.dismiss();
  }
}
