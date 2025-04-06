import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { IonContent, IonHeader, IonTitle, IonToolbar } from '@ionic/angular/standalone';
import { Router } from '@angular/router';

@Component({
  selector: 'app-policy',
  templateUrl: './policy.page.html',
  styleUrls: ['./policy.page.scss'],
  standalone: true,
  imports: [IonContent, CommonModule, FormsModule]
})
export class PolicyPage implements OnInit {

  constructor(private router: Router) {}

  ngOnInit() {
  }

  navigateToTerms(event: Event): void {
    event.preventDefault(); // Prevents the default anchor behavior
    this.router.navigate(['/terms']);
  }

}
