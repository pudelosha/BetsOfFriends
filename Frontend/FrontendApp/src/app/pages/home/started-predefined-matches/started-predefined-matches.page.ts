import { Component, Input, Output, EventEmitter, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToastController } from '@ionic/angular';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { Match } from 'src/app/model/match';
import { PredefinedMatchService } from 'src/app/services/predefined-match.service';
import { TranslateModule } from '@ngx-translate/core';
import { IonList, IonItem } from '@ionic/angular/standalone';

@Component({
  selector: 'app-started-predefined-matches',
  standalone: true,
  imports: [CommonModule, TranslateModule, IonList, IonItem],
  templateUrl: './started-predefined-matches.page.html',
  styleUrls: ['./started-predefined-matches.page.scss']
})
export class StartedPredefinedMatchesPage implements OnChanges {
  @Input() refreshTrigger: number = 0;
  @Output() loadingStart = new EventEmitter<void>();
  @Output() loadingEnd = new EventEmitter<void>();

  startedMatches: Match[] = [];
  isLoading = true;
  errorMessage: string | null = null;

  constructor(
    private matchService: PredefinedMatchService,
    private toastController: ToastController,
    private router: Router
  ) {}

  async ionViewWillEnter() {
    await this.loadStartedMatches();
  }

  async ngOnChanges(changes: SimpleChanges) {
    if (changes['refreshTrigger'] && !changes['refreshTrigger'].firstChange) {
      this.loadStartedMatches();
    }
  }

  async loadStartedMatches() {
    this.loadingStart.emit();
    this.isLoading = true;
    this.startedMatches = [];
    this.errorMessage = '';
  
    try {
      this.startedMatches = await firstValueFrom(
        this.matchService.getStartedMatches()
      );
  
      if (!this.startedMatches.length) {
        this.errorMessage = 'No started matches found.';
      }
    } catch (error) {
      console.error('Error loading started predefined matches:', error);
      this.errorMessage = 'Failed to load matches.';
    } finally {
      this.isLoading = false;
      this.loadingEnd.emit();
    }
  }
  
  navigateToPredefinedMatches() {
    this.router.navigate(['/tournaments/predefined']);
  }

  async showToast(message: string, color: 'success' | 'warning' | 'danger') {
    const toast = await this.toastController.create({
      message,
      duration: 3000,
      position: 'bottom',
      color,
    });
    await toast.present();
  }
}
