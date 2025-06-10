
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { Router } from '@angular/router';
import { TitleService } from 'src/app/services/title.service';
import { IonContent, IonIcon } from '@ionic/angular/standalone';

@Component({
  selector: 'app-info-and-support',
  templateUrl: './info-and-support.page.html',
  styleUrls: ['./info-and-support.page.scss'],
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, TranslateModule, IonContent, IonIcon],
})
export class InfoAndSupportPage implements OnInit {

  constructor(private router: Router, private titleService: TitleService) {}

  ngOnInit() {
    this.titleService.setTitle('INFO.TITLE');
  }

  ionViewWillEnter() {
    this.titleService.setTitle('INFO.TITLE');
  }

  navigateTo(route: string): void {
    this.router.navigate([route]);
  }
}