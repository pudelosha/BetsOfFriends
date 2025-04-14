import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { IonContent, IonHeader, IonTitle, IonToolbar } from '@ionic/angular/standalone';
import { TitleService } from 'src/app/services/title.service'; // adjust path if needed

@Component({
  selector: 'app-terms',
  templateUrl: './terms.page.html',
  styleUrls: ['./terms.page.scss'],
  standalone: true,
  imports: [IonContent, CommonModule, FormsModule]
})
export class TermsPage implements OnInit {

  constructor(private titleService: TitleService) {}

  ngOnInit() {
    this.titleService.setTitle('TERMS.TITLE'); // sets title when component initializes
  }

  ionViewWillEnter() {
    this.titleService.setTitle('TERMS.TITLE'); // ensures it's re-set when navigating back
  }

}
