import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { NotificationDto } from 'src/app/model/notification';
import { NotificationService } from 'src/app/services/notification.service';
import { Component, OnInit } from '@angular/core';
import { ToastController, AlertController, LoadingController } from '@ionic/angular';
import { CommonModule } from '@angular/common';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { TitleService } from 'src/app/services/title.service';
import { Router } from '@angular/router';
import { IonContent, IonSpinner, IonList, IonItem, IonButton, IonIcon, IonAccordionGroup, IonAccordion, IonGrid, IonRow, IonCol } from '@ionic/angular/standalone';

@Component({
  selector: 'app-messages',
  templateUrl: './messages.page.html',
  styleUrls: ['./messages.page.scss'],
  standalone: true,
  imports: [IonCol, IonRow, IonGrid, CommonModule, ReactiveFormsModule, FormsModule, TranslateModule, IonContent, IonSpinner, IonList, IonItem, IonButton, IonIcon, IonAccordionGroup, IonAccordion],
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
    private titleService: TitleService,
    private router: Router,
    private translate: TranslateService
  ) {}

  ngOnInit() {
    //this.loadNotifications();
  }

  ionViewWillEnter() {
    this.expandedNotificationId = null;
    this.titleService.setTitle('MESSAGES.TITLE');
    this.loadNotifications();
  }

  openNotificationLink(notification: NotificationDto) {
    console.log('Opening notification:', notification);
  
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
        this.totalPages = Math.ceil(data.length / this.pageSize);
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
        this.notifications = this.notifications.filter(n => n.notificationId !== notification.notificationId);
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
