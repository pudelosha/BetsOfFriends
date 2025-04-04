import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { NotificationDto } from 'src/app/model/notification';
import { NotificationService } from 'src/app/services/notification.service';
import { Component, OnInit } from '@angular/core';
import { ToastController, AlertController, LoadingController } from '@ionic/angular';
import { CommonModule } from '@angular/common';
import { IonicModule } from '@ionic/angular';
import { TranslateModule } from '@ngx-translate/core';
import { TitleService } from 'src/app/services/title.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-messages',
  templateUrl: './messages.page.html',
  styleUrls: ['./messages.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule, FormsModule, TranslateModule],
})
export class MessagesPage implements OnInit {
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
     
  // Load notifications from API
  async loadNotifications() {
    this.isLoading = true;
  
    const loading = await this.loadingController.create({
      message: 'Loading notifications...',
      spinner: 'crescent',
    });
    await loading.present(); // Show loading UI
  
    const startTime = Date.now(); // Track when loading starts
  
    this.notificationService.getNotifications().subscribe({
      next: async (data) => {
        this.notifications = data;
        this.expandedNotificationId = null; // Ensure all accordions are collapsed
  
        // Ensure the spinner stays visible for at least 1 second
        const elapsedTime = Date.now() - startTime;
        const delay = Math.max(0, 1000 - elapsedTime);
  
        setTimeout(async () => {
          await loading.dismiss();
          this.isLoading = false;
        }, delay);
      },
      error: async (error) => {
        console.error('Error fetching notifications:', error);
        this.showToast('Failed to load notifications', 'danger');
  
        const elapsedTime = Date.now() - startTime;
        const delay = Math.max(0, 500 - elapsedTime);
  
        setTimeout(async () => {
          await loading.dismiss();
          this.isLoading = false;
        }, delay);
      }
    });
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
