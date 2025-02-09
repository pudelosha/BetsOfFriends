import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { IonContent, IonHeader, IonTitle, IonToolbar } from '@ionic/angular/standalone';

@Component({
  selector: 'app-my-tournaments',
  templateUrl: './my-tournaments.page.html',
  styleUrls: ['./my-tournaments.page.scss'],
  standalone: true,
  imports: [IonContent, IonHeader, IonTitle, IonToolbar, CommonModule, FormsModule]
})
export class MyTournamentsPage implements OnInit {

  constructor() { }

  ngOnInit() {
  }

}
