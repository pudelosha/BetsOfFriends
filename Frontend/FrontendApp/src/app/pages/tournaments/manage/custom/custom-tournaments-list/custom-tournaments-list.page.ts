import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { IonContent, IonHeader, IonTitle, IonToolbar } from '@ionic/angular/standalone';

@Component({
  selector: 'app-custom-tournaments-list',
  templateUrl: './custom-tournaments-list.page.html',
  styleUrls: ['./custom-tournaments-list.page.scss'],
  standalone: true,
  imports: [IonContent, IonHeader, IonTitle, IonToolbar, CommonModule, FormsModule]
})
export class CustomTournamentsListPage implements OnInit {

  constructor() { }

  ngOnInit() {
  }

}
