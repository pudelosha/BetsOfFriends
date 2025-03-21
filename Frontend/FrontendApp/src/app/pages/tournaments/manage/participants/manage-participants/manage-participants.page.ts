import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { IonContent, IonHeader, IonTitle, IonToolbar } from '@ionic/angular/standalone';

@Component({
  selector: 'app-manage-participants',
  templateUrl: './manage-participants.page.html',
  styleUrls: ['./manage-participants.page.scss'],
  standalone: true,
  imports: [IonContent, IonHeader, IonTitle, IonToolbar, CommonModule, FormsModule]
})
export class ManageParticipantsPage implements OnInit {

  constructor() { }

  ngOnInit() {
  }

}
