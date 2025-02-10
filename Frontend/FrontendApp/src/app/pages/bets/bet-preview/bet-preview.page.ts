import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { IonContent, IonHeader, IonTitle, IonToolbar } from '@ionic/angular/standalone';

@Component({
  selector: 'app-bet-preview',
  templateUrl: './bet-preview.page.html',
  styleUrls: ['./bet-preview.page.scss'],
  standalone: true,
  imports: [CommonModule, FormsModule]
})
export class BetPreviewPage implements OnInit {

  constructor() { }

  ngOnInit() {
  }

}
