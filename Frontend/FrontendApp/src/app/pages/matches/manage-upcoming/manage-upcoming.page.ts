import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { IonContent, IonHeader, IonTitle, IonToolbar } from '@ionic/angular/standalone';

@Component({
  selector: 'app-manage-upcoming',
  templateUrl: './manage-upcoming.page.html',
  styleUrls: ['./manage-upcoming.page.scss'],
  standalone: true,
  imports: [IonContent, IonHeader, IonTitle, IonToolbar, CommonModule, FormsModule]
})
export class ManageUpcomingPage implements OnInit {

  constructor() { }

  ngOnInit() {
  }

}
