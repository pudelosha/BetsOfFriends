import { Component, OnInit, Input, Output, EventEmitter, SimpleChanges, OnChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { ModalController, ToastController } from '@ionic/angular';
import { TournamentSelectionService } from 'src/app/services/tournament-selection.service';
import { firstValueFrom } from 'rxjs';
import { TournamentMessage } from 'src/app/model/message';
import { TournamentMessageService } from 'src/app/services/tournament-message.service';
import { IonSpinner } from "@ionic/angular/standalone";
import { TournamentMessageModalComponent } from 'src/app/modals/tournament-message-modal/tournament-message-modal.component';

@Component({
  selector: 'app-tournament-message-board',
  templateUrl: './tournament-message-board.page.html',
  styleUrls: ['./tournament-message-board.page.scss'],
  standalone: true,
  imports: [IonSpinner, CommonModule, FormsModule, TranslateModule],
})
export class TournamentMessageBoardPage implements OnInit, OnChanges {
  @Input() refreshTrigger: number = 0;
  @Output() loadingStart = new EventEmitter<void>();
  @Output() loadingEnd = new EventEmitter<void>();

  messages: TournamentMessage[] = [];
  isLoading = true;

  constructor(
    private tournamentSelectionService: TournamentSelectionService,
    private tournamentMessageService: TournamentMessageService,
    private modalController: ModalController,
    private toastController: ToastController
  ) {}

  ngOnInit() {}

  async ionViewWillEnter() {
    await this.loadMessages();
  }

  async ngOnChanges(changes: SimpleChanges) {
    if (changes['refreshTrigger'] && !changes['refreshTrigger'].firstChange) {
      await this.loadMessages();
    }
  }

  async loadMessages() {
    this.loadingStart.emit();
    this.isLoading = true;

    const tournamentId = this.tournamentSelectionService.getSelectedTournament();

    if (!tournamentId) {
      console.warn("No tournament selected.");
      this.messages = [];
      this.isLoading = false;
      this.loadingEnd.emit();
      return;
    }

    try {
      this.messages = await firstValueFrom(
        this.tournamentMessageService.getMessages(tournamentId)
      );
    } catch (error) {
      console.error('Failed to load tournament messages:', error);
    } finally {
      this.isLoading = false;
      this.loadingEnd.emit();
    }
  }

  async openMessageBoardModal() {
    const modal = await this.modalController.create({
      component: TournamentMessageModalComponent,
      breakpoints: [0, 0.4, 0.7],
      initialBreakpoint: 0.35,
    });

    await modal.present();

    const { data, role } = await modal.onWillDismiss();

    if (data && data.content) {
      try {
        const tournamentId = this.tournamentSelectionService.getSelectedTournament();
        if (tournamentId) {
          await firstValueFrom(
            this.tournamentMessageService.postMessage(tournamentId, data.content)
          );
          await this.loadMessages();
          await this.showToast('Message posted successfully!', 'success');
        }
      } catch (err: any) {
        console.error('Failed to post message', err);
        if (err?.status === 400 && err?.error?.message) {
          await this.showToast(err.error.message, 'warning');
        } else {
          await this.showToast('Failed to post message.', 'danger');
        }
      }
    }
  }

  private async showToast(message: string, color: 'success' | 'warning' | 'danger') {
    const toast = await this.toastController.create({
      message,
      duration: 3000,
      position: 'bottom',
      color,
    });
    await toast.present();
  }
}
