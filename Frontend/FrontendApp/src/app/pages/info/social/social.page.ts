import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonicModule } from '@ionic/angular';
import { TranslateModule } from '@ngx-translate/core';
import { TitleService } from 'src/app/services/title.service';

@Component({
  selector: 'app-social',
  templateUrl: './social.page.html',
  styleUrls: ['./social.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule, FormsModule, TranslateModule],
})
export class SocialPage implements OnInit {

  constructor(private titleService: TitleService) { }

  ngOnInit() {
    this.titleService.setTitle('SOCIAL.TITLE');
  }

  ionViewWillEnter() {
    this.titleService.setTitle('SOCIAL.TITLE');
  }

  openLink(url: string): void {
    window.open(url, '_blank');
  }

}
