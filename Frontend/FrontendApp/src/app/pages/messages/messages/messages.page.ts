import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { NotificationDto } from 'src/app/model/notification';
import { NotificationService } from 'src/app/services/notification.service';
import { Component, OnInit } from '@angular/core';
import { ToastController, AlertController, LoadingController, ModalController } from '@ionic/angular';
import { CommonModule } from '@angular/common';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { TitleService } from 'src/app/services/title.service';
import { Router } from '@angular/router';
import { TournamentSelectionService } from 'src/app/services/tournament-selection.service';
import { IonContent, IonSpinner, IonList, IonItem, IonButton, IonIcon, IonAccordionGroup, IonAccordion } from '@ionic/angular/standalone';
import { firstValueFrom } from 'rxjs';
import { DeleteAllMessagesModalComponent } from 'src/app/modals/delete-all-messages-modal/delete-all-messages-modal.component';
import { MessageComposeModalComponent } from 'src/app/modals/message-compose-modal/message-compose-modal.component';
import { MessageTextModalComponent } from 'src/app/modals/message-text-modal/message-text-modal.component';

@Component({
  selector: 'app-messages',
  templateUrl: './messages.page.html',
  styleUrls: ['./messages.page.scss'],
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, TranslateModule, IonContent, IonSpinner, IonList, IonItem, IonButton, IonIcon, IonAccordionGroup, IonAccordion],
})
export class MessagesPage implements OnInit {
  currentPage = 1;
  pageSize = 20;
  totalPages = 1;
  allNotifications: NotificationDto[] = [];
  notifications: NotificationDto[] = [];
  expandedNotificationId: number | null = null;
  isLoading = true;

  constructor(
    private notificationService: NotificationService,
    private toastController: ToastController,
    private alertController: AlertController,
    private loadingController: LoadingController,
    private modalController: ModalController,
    private titleService: TitleService,
    private router: Router,
    private translate: TranslateService,
    private tournamentSelectionService: TournamentSelectionService
  ) {}

  ngOnInit() {
    //this.loadNotifications();
  }

  ionViewWillEnter() {
    this.expandedNotificationId = null;
    this.titleService.setTitle('MESSAGES.TITLE');
    this.loadNotifications();
  }

  async openNotificationLink(notification: NotificationDto) {
    console.log('Opening notification:', notification);

    if (this.isReplyableNotification(notification)) {
      await this.openReplyModal(notification);
      return;
    }
  
    if (notification.route) {
      this.router.navigateByUrl(notification.route)
        .catch(err => console.error('Navigation error:', err));
    }
  }
       
  loadNotifications() {
    this.isLoading = true;
    this.notificationService.getNotifications().subscribe({
      next: (data) => {
        this.allNotifications = data;
        this.totalPages = Math.max(1, Math.ceil(data.length / this.pageSize));
        if (this.currentPage > this.totalPages) {
          this.currentPage = this.totalPages;
        }
        this.updatePage();
      },
      error: (err) => {
        console.error(err);
        this.showToast(this.t('TOASTS.NOTIFICATIONS_LOAD_FAILED'), 'danger');
        this.isLoading = false;
      }
    });
  }
  
  updatePage() {
    const start = (this.currentPage - 1) * this.pageSize;
    this.notifications = this.allNotifications.slice(start, start + this.pageSize);
    this.expandedNotificationId = null;
    this.isLoading = false;
  }
  
  nextPage() {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
      this.updatePage();
    }
  }
  
  prevPage() {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.updatePage();
    }
  }
    
  toggleNotification(notification: NotificationDto) {
    if (this.expandedNotificationId === notification.notificationId) {
      this.expandedNotificationId = null;
    } else {
      this.expandedNotificationId = notification.notificationId;

      if (!notification.isRead) {
        this.notificationService.markAsRead(notification.notificationId).subscribe({
          next: () => {
            notification.isRead = true;
          },
          error: (error) => console.error('Error marking as read:', error)
        });
      }
    }
  }

  async confirmDelete(notification: NotificationDto) {
    const alert = await this.alertController.create({
      header: this.t('TOASTS.DELETE_NOTIFICATION_TITLE'),
      message: this.t('TOASTS.DELETE_NOTIFICATION_CONFIRM'),
      buttons: [
        {
          text: this.t('TOASTS.CANCEL'),
          role: 'cancel'
        },
        {
          text: this.t('TOASTS.DELETE'),
          role: 'destructive',
          handler: () => this.deleteNotification(notification)
        }
      ]
    });

    await alert.present();
  }

  async deleteNotification(notification: NotificationDto) {
    const loading = await this.loadingController.create({
      message: this.t('TOASTS.DELETING_NOTIFICATION'),
      spinner: 'crescent',
    });
    await loading.present();
  
    const startTime = Date.now();
  
    this.notificationService.deleteNotification(notification.notificationId).subscribe({
      next: async () => {
        this.allNotifications = this.allNotifications.filter(n => n.notificationId !== notification.notificationId);
        this.totalPages = Math.max(1, Math.ceil(this.allNotifications.length / this.pageSize));
        if (this.currentPage > this.totalPages) {
          this.currentPage = this.totalPages;
        }
        this.updatePage();
        this.expandedNotificationId = null;
  
        const elapsedTime = Date.now() - startTime;
        const delay = Math.max(0, 500 - elapsedTime);
  
        setTimeout(async () => {
          await loading.dismiss();
          this.showToast(this.t('TOASTS.NOTIFICATION_DELETED'), 'success');
        }, delay);
      },
      error: async (error) => {
        console.error('Error deleting notification:', error);
        const elapsedTime = Date.now() - startTime;
        const delay = Math.max(0, 1000 - elapsedTime);
  
        setTimeout(async () => {
          await loading.dismiss();
          this.showToast(this.t('TOASTS.NOTIFICATION_DELETE_FAILED'), 'danger');
        }, delay);
      }
    });
  }

  async openComposeModal() {
    const tournamentId = this.tournamentSelectionService.getSelectedTournament();
    if (!tournamentId) {
      this.showToast(this.t('TOASTS.SELECT_TOURNAMENT_FIRST'), 'danger');
      return;
    }

    const loading = await this.loadingController.create({
      message: this.t('TOASTS.LOADING'),
      spinner: 'crescent',
    });
    await loading.present();

    try {
      const recipients = await firstValueFrom(this.notificationService.getTournamentMessageRecipients(tournamentId));
      await loading.dismiss();

      const modal = await this.modalController.create({
        component: MessageComposeModalComponent,
        componentProps: {
          recipients
        },
        breakpoints: [0, 0.75, 1],
        initialBreakpoint: 0.75,
      });

      await modal.present();
      const { data } = await modal.onWillDismiss();

      if (data?.recipientAssignmentId && data?.message) {
        this.sendMessage(tournamentId, data.recipientAssignmentId, data.message);
      }
    } catch (error) {
      await loading.dismiss();
      console.error('Error loading message recipients:', error);
      this.showToast(this.t('TOASTS.MESSAGE_RECIPIENTS_LOAD_FAILED'), 'danger');
    }
  }

  sendMessage(tournamentId: number, recipientAssignmentId: number, message: string) {
    if (!tournamentId || !recipientAssignmentId || !message.trim()) {
      this.showToast(this.t('TOASTS.MESSAGE_REQUIRED_FIELDS'), 'danger');
      return;
    }

    this.notificationService.sendTournamentUserMessage({
      tournamentId,
      recipientAssignmentId,
      message: message.trim()
    }).subscribe({
      next: () => {
        this.showToast(this.t('TOASTS.MESSAGE_SENT'), 'success');
      },
      error: (error) => {
        console.error('Error sending message:', error);
        this.showToast(this.t('TOASTS.MESSAGE_SEND_FAILED'), 'danger');
      }
    });
  }

  async confirmDeleteAll() {
    const modal = await this.modalController.create({
      component: DeleteAllMessagesModalComponent,
      breakpoints: [0, 0.5, 1],
      initialBreakpoint: 0.5,
    });

    await modal.present();
    const { data } = await modal.onWillDismiss();

    if (data?.confirmed) {
      this.deleteAllNotifications();
    }
  }

  async deleteAllNotifications() {
    const loading = await this.loadingController.create({
      message: this.t('TOASTS.DELETING_NOTIFICATION'),
      spinner: 'crescent',
    });
    await loading.present();

    this.notificationService.deleteAllNotifications().subscribe({
      next: async () => {
        this.allNotifications = [];
        this.notifications = [];
        this.currentPage = 1;
        this.totalPages = 1;
        this.expandedNotificationId = null;
        await loading.dismiss();
        this.showToast(this.t('TOASTS.ALL_MESSAGES_DELETED'), 'success');
      },
      error: async (error) => {
        console.error('Error deleting all notifications:', error);
        await loading.dismiss();
        this.showToast(this.t('TOASTS.NOTIFICATION_DELETE_FAILED'), 'danger');
      }
    });
  }

  private isReplyableNotification(notification: NotificationDto): boolean {
    return !!notification.senderUserId &&
      (notification.type === 'UserMessage' || notification.type === 'AdminBroadcast' || notification.type === 'AdminDirectMessage');
  }

  private async openReplyModal(notification: NotificationDto): Promise<void> {
    if (!notification.senderUserId) {
      return;
    }

    const modal = await this.modalController.create({
      component: MessageTextModalComponent,
      componentProps: {
        titleKey: 'MESSAGES.REPLY_TITLE',
        titleParams: {
          name: notification.senderDisplayName || notification.title
        },
        descriptionKey: 'MESSAGES.REPLY_DESCRIPTION',
        descriptionParams: {
          name: notification.senderDisplayName || notification.title
        },
        submitKey: 'MESSAGES.SEND'
      },
      breakpoints: [0, 0.65, 1],
      initialBreakpoint: 0.65,
    });

    await modal.present();
    const { data } = await modal.onWillDismiss();

    if (!data?.message) {
      return;
    }

    this.notificationService.replyToUserMessage({
      recipientUserId: notification.senderUserId,
      tournamentId: notification.tournamentId ?? null,
      message: data.message
    }).subscribe({
      next: () => {
        this.showToast(this.t('TOASTS.MESSAGE_SENT'), 'success');
      },
      error: (error) => {
        console.error('Error replying to message:', error);
        this.showToast(this.t('TOASTS.MESSAGE_SEND_FAILED'), 'danger');
      }
    });
  }
  
  private async showToast(message: string, color: 'success' | 'danger') {
    const toast = await this.toastController.create({
      message,
      duration: 2000,
      position: 'bottom',
      color,
    });
    await toast.present();
  }

  private t(key: string, params?: Record<string, unknown>): string {
    return this.translate.instant(key, params);
  }
}
