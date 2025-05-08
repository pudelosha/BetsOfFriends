import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { NotificationDto } from 'src/app/model/notification';
import { NotificationService } from 'src/app/services/notification.service';
import { Component, OnInit } from '@angular/core';
import { ToastController, AlertController, LoadingController } from '@ionic/angular';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
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
    private router: Router
  ) {}

  ngOnInit() {
    this.titleService.setTitle('MESSAGES.TITLE');
    this.loadNotifications();
  }

  ionViewWillEnter() {
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
        this.showToast('Failed to load notifications', 'danger');
        this.isLoading = false;
      }
    });
  }
  
  updatePage() {
    const start = (this.currentPage - 1) * this.pageSize;
    this.notifications = this.allNotifications.slice(start, start + this.pageSize);
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
    
  // Toggle message expansion and mark as read
  toggleNotification(notification: NotificationDto) {
    if (this.expandedNotificationId === notification.notificationId) {
      this.expandedNotificationId = null;  // Collapse if already expanded
    } else {
      this.expandedNotificationId = notification.notificationId;  // Expand new one

      if (!notification.isRead) {
        this.notificationService.markAsRead(notification.notificationId).subscribe({
          next: () => {
            notification.isRead = true;  // Update UI after marking as read
          },
          error: (error) => console.error('Error marking as read:', error)
        });
      }
    }
  }

  // Confirm delete notification
  async confirmDelete(notification: NotificationDto) {
    const alert = await this.alertController.create({
      header: 'Delete Notification',
      message: 'Are you sure you want to delete this notification?',
      buttons: [
        {
          text: 'Cancel',
          role: 'cancel'
        },
        {
          text: 'Delete',
          role: 'destructive',
          handler: () => this.deleteNotification(notification)
        }
      ]
    });

    await alert.present();
  }

  // Delete a notification
  async deleteNotification(notification: NotificationDto) {
    const loading = await this.loadingController.create({
      message: 'Deleting notification...',
      spinner: 'crescent',
    });
    await loading.present(); // Show loading UI
  
    const startTime = Date.now();
  
    this.notificationService.deleteNotification(notification.notificationId).subscribe({
      next: async () => {
        this.notifications = this.notifications.filter(n => n.notificationId !== notification.notificationId);
        this.expandedNotificationId = null; // Collapse everything after deletion
  
        const elapsedTime = Date.now() - startTime;
        const delay = Math.max(0, 500 - elapsedTime);
  
        setTimeout(async () => {
          await loading.dismiss();
          this.showToast('Notification deleted', 'success');
        }, delay);
      },
      error: async (error) => {
        console.error('Error deleting notification:', error);
        const elapsedTime = Date.now() - startTime;
        const delay = Math.max(0, 1000 - elapsedTime);
  
        setTimeout(async () => {
          await loading.dismiss();
          this.showToast('Failed to delete notification', 'danger');
        }, delay);
      }
    });
  }
  
  // Display a toast message
  private async showToast(message: string, color: 'success' | 'danger') {
    const toast = await this.toastController.create({
      message,
      duration: 2000,
      position: 'bottom',
      color,
    });
    await toast.present();
  }
}
