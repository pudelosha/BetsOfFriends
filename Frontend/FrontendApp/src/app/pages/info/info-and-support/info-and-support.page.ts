
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonicModule } from '@ionic/angular';
import { TranslateModule } from '@ngx-translate/core';
import { Router } from '@angular/router';


@Component({
  selector: 'app-info-and-support',
  templateUrl: './info-and-support.page.html',
  styleUrls: ['./info-and-support.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule, FormsModule, TranslateModule],
})
export class InfoAndSupportPage implements OnInit {

  constructor(private router: Router) {}

  ngOnInit() {
  }

  navigateTo(route: string): void {
    this.router.navigate([route]);
  }

}
