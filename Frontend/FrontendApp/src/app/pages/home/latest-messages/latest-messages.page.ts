import { Component, Input, Output, EventEmitter, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NotificationService } from 'src/app/services/notification.service';
import { NotificationDto } from 'src/app/model/notification';
import { firstValueFrom } from 'rxjs';
import { ReactiveFormsModule } from '@angular/forms';
import { ToastController } from '@ionic/angular';
import { Router } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { IonList, IonItem } from '@ionic/angular/standalone';

@Component({
  selector: 'app-latest-messages',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TranslateModule, IonList, IonItem],
  templateUrl: './latest-messages.page.html',
  styleUrls: ['./latest-messages.page.scss']
})
export class LatestMessagesPage implements OnChanges {
  @Input() refreshTrigger: number = 0;
  @Output() loadingStart = new EventEmitter<void>();
  @Output() loadingEnd = new EventEmitter<void>();

  messages: NotificationDto[] = [];
  isLoading = true;
  errorMessage: string | null = null;

  constructor(
    private notificationService: NotificationService,
    private toastController: ToastController,
    private router: Router
  ) {}

  async ionViewWillEnter() {
    await this.loadMessages();
  }

  async ngOnChanges(changes: SimpleChanges) {
    if (changes['refreshTrigger'] && !changes['refreshTrigger'].firstChange) {
      this.loadMessages();
    }
  }

  async loadMessages() {
    this.loadingStart.emit();

    try {
      this.messages = await firstValueFrom(this.notificationService.getLatestNotifications()) as NotificationDto[];
    } catch (error) {
      console.error('Error fetching messages:', error);
      this.errorMessage = 'Failed to load messages.';
    } finally {
      this.isLoading = false;
      this.loadingEnd.emit();
    }
  }

  goToMessages() {
    this.router.navigate(['/messages']);
  }

  async showToast(message: string, color: 'success' | 'warning' | 'danger' | 'primary') {
    const toast = await this.toastController.create({
      message,
      duration: 3000,
      position: 'bottom',
      color,
    });
    await toast.present();
  }
}
