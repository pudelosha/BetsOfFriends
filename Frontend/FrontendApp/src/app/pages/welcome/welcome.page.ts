import { Component, OnInit, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { IonContent, IonButton } from '@ionic/angular/standalone';
import { Router } from '@angular/router';
import { NavController } from '@ionic/angular';

@Component({
  selector: 'app-welcome',
  templateUrl: './welcome.page.html',
  styleUrls: ['./welcome.page.scss'],
  standalone: true,
  imports: [IonContent, IonButton, CommonModule, FormsModule]
})
export class WelcomePage {
  parallaxOffset = 0;

  tiles = [
    {
      image: '/assets/images/tile1.jpg',
      title: 'Create Custom Tournaments',
      description: 'Build your own competitions and invite your friends to join the action!',
    },
    {
      image: '/assets/images/tile2.jpg',
      title: 'Predict Match Outcomes',
      description: 'Make your predictions and compete for the top spot in the rankings!',
    },
    {
      image: '/assets/images/tile3.jpg',
      title: 'Live Score Updates',
      description: 'Stay updated with real-time scores and match results as they happen!',
    },
  ];

  constructor(private navCtrl: NavController) {}

  @HostListener('window:scroll', ['$event'])
  onScroll(): void {
    const scrollY = window.scrollY || document.documentElement.scrollTop;
    this.parallaxOffset = scrollY * 0.4; // Adjust speed
  }

  navigateToLogin() {
    this.navCtrl.navigateForward('/login');
  }

  navigateToRegister() {
    this.navCtrl.navigateForward('/register');
  }
}
