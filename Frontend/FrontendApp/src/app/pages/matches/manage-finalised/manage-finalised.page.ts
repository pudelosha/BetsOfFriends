import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { IonContent, IonHeader, IonTitle, IonToolbar } from '@ionic/angular/standalone';

@Component({
  selector: 'app-manage-finalised',
  templateUrl: './manage-finalised.page.html',
  styleUrls: ['./manage-finalised.page.scss'],
  standalone: true,
  imports: [IonContent, IonHeader, IonTitle, IonToolbar, CommonModule, FormsModule]
})
export class ManageFinalisedPage implements OnInit {

  constructor() { }

  ngOnInit() {
  }

}
