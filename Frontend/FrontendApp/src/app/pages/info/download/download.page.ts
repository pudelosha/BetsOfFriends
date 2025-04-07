import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonicModule } from '@ionic/angular';
import { TranslateModule } from '@ngx-translate/core';
import { DownloadService } from 'src/app/services/download.service';
import { TitleService } from 'src/app/services/title.service';


@Component({
  selector: 'app-download',
  templateUrl: './download.page.html',
  styleUrls: ['./download.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule, FormsModule, TranslateModule],
})
export class DownloadPage {
  
  constructor(
    private downloadService: DownloadService,
    private titleService: TitleService
  ) {}

  ngOnInit(): void {
    this.titleService.setTitle('DOWNLOAD.TITLE');
  }

  ionViewWillEnter(): void {
    this.titleService.setTitle('DOWNLOAD.TITLE');
  }

  downloadFile(fileName: string): void {
    this.downloadService.downloadFile(fileName);
  }

  openStore(platform: 'ios' | 'android'): void {
    this.downloadService.openStore(platform);
  }
}
