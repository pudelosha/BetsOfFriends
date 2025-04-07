import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { IonContent, IonHeader, IonTitle, IonToolbar } from '@ionic/angular/standalone';
import { Router } from '@angular/router';
import { TitleService } from 'src/app/services/title.service';


@Component({
  selector: 'app-policy',
  templateUrl: './policy.page.html',
  styleUrls: ['./policy.page.scss'],
  standalone: true,
  imports: [IonContent, CommonModule, FormsModule]
})
export class PolicyPage implements OnInit {

  constructor(
    private router: Router,
    private titleService: TitleService
  ) {}

  ngOnInit() {
    this.titleService.setTitle('POLICY.TITLE');
  }

  ionViewWillEnter() {
    this.titleService.setTitle('POLICY.TITLE');
  }

  navigateToTerms(event: Event): void {
    event.preventDefault();
    this.router.navigate(['/terms']);
  }

}
