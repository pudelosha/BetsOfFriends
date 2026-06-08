import { Component, Input, OnInit, OnChanges, SimpleChanges  } from '@angular/core';
import { ModalController, ToastController, LoadingController } from '@ionic/angular';
import { EditMatchResultModalComponent } from 'src/app/modals/edit-match-result-modal/edit-match-result-modal.component';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { PredefinedMatchService } from 'src/app/services/predefined-match.service';
import { Match } from 'src/app/model/match';
import { HttpErrorResponse } from '@angular/common/http';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { IonSpinner, IonList, IonItem, IonButton } from '@ionic/angular/standalone';

@Component({
  selector: 'app-manage-predefined-finalised',
  templateUrl: './manage-predefined-finalised.page.html',
  styleUrls: ['./manage-predefined-finalised.page.scss'],
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, TranslateModule, IonSpinner, IonList, IonItem, IonButton],
})
export class ManagePredefinedFinalisedPage implements OnInit, OnChanges {
  @Input() stage!: string;
  @Input() tournamentId!: number;

  matches: Match[] = [];
  isLoading = true;
  errorMessage: string = '';

  constructor(
    private modalCtrl: ModalController,
    private matchService: PredefinedMatchService,
    private toastController: ToastController,
    private loadingController: LoadingController,
    private translate: TranslateService
  ) {}

  ngOnInit() {
    this.loadMatches(); 
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['stage'] && !changes['stage'].firstChange) {
      this.loadMatches();
    }
  }  
  
  ionViewWillEnter() {
    this.loadMatches(); 
  }
  
  async loadMatches() {
    this.isLoading = true;
    this.matches = [];
    this.errorMessage = '';
  
    const loading = await this.loadingController.create({
      message: this.t('TOASTS.LOADING_MATCHES'),
      spinner: 'crescent',
    });
    await loading.present();
  
    const startTime = Date.now();
  
    if (!this.tournamentId) {
      this.errorMessage = this.t('TOASTS.NO_TOURNAMENT_ID');
      this.isLoading = false;
      await loading.dismiss();
      return;
    }
  
    try {
      this.matches = await firstValueFrom(
        this.matchService.getMatchesByTournamentStage(this.tournamentId, 'Finished', this.stage)
      );
  
      if (!this.matches.length) {
        this.errorMessage = this.t('TOASTS.NO_MATCHES_FOR_STAGE');
      }
    } catch (error: unknown) {
      if (error instanceof HttpErrorResponse) {
        this.errorMessage = this.t('TOASTS.ERROR_OCCURRED', { message: error.message });
      } else {
        this.errorMessage = this.t('TOASTS.UNEXPECTED_ERROR');
      }
    } finally {
      const elapsedTime = Date.now() - startTime;
      const delay = Math.max(0, 200 - elapsedTime);
      setTimeout(async () => {
        this.isLoading = false;
        await loading.dismiss();
      }, delay);
    }
  }
      
  async editMatchResult(match: Match, event: Event) {
    event.stopPropagation();
  
    const modal = await this.modalCtrl.create({
      component: EditMatchResultModalComponent,
      componentProps: { match },
      breakpoints: [0, 0.5, 0.75, 1],
      initialBreakpoint: 1,
    });
  
    await modal.present();
  
    const { data } = await modal.onWillDismiss();
    if (data) {
  
      try {
        await firstValueFrom(this.matchService.updateMatchResult(match.matchId, data));
  
        await this.loadMatches();  
  
        this.showToast(this.t('TOASTS.MATCH_RESULT_UPDATED'), "success");
      } catch (error) {
        this.showToast(this.t('TOASTS.MATCH_RESULT_UPDATE_FAILED'), "danger");
      }
    }
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

  private t(key: string, params?: Record<string, unknown>): string {
    return this.translate.instant(key, params);
  }
}
