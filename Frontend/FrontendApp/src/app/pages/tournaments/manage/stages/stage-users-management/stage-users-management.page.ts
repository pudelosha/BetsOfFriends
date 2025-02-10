import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { IonContent, IonHeader, IonTitle, IonToolbar } from '@ionic/angular/standalone';

@Component({
  selector: 'app-stage-users-management',
  templateUrl: './stage-users-management.page.html',
  styleUrls: ['./stage-users-management.page.scss'],
  standalone: true,
  imports: [CommonModule, FormsModule]
})
export class StageUsersManagementPage implements OnInit {

  constructor() { }

  ngOnInit() {
  }

}
