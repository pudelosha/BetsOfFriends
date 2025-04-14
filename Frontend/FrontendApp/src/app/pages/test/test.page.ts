import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonHeader, IonToolbar, IonTitle, IonContent } from '@ionic/angular/standalone';


@Component({
  standalone: true,
  selector: 'app-test',
  imports: [CommonModule, IonHeader, IonToolbar, IonTitle, IonContent],
  template: `
    <ion-header>
      <ion-toolbar>
        <ion-title>Hello World</ion-title>
      </ion-toolbar>
    </ion-header>
    <ion-content fullscreen>
      <div style="text-align: center; padding: 2rem;">
        <h1>This is working!</h1>
      </div>
    </ion-content>
  `
})
export class TestPage {


  

  
}
