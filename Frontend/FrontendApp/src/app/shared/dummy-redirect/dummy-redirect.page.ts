import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { IonContent, IonHeader, IonTitle, IonToolbar } from '@ionic/angular/standalone';

@Component({
  selector: 'app-dummy-redirect',
  templateUrl: './dummy-redirect.page.html',
  styleUrls: ['./dummy-redirect.page.scss'],
  standalone: true,
  imports: [IonContent, IonHeader, IonTitle, IonToolbar, CommonModule, FormsModule]
})
export class DummyRedirectPage implements OnInit {

  constructor() { }

  ngOnInit() {
  }

}
