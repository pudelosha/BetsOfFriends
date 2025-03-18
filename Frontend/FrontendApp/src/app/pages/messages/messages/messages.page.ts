import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { NotificationDto } from 'src/app/model/notification';
import { NotificationService } from 'src/app/services/notification.service';
import { Component, OnInit } from '@angular/core';
import { ToastController, AlertController } from '@ionic/angular';
import { CommonModule } from '@angular/common';
import { IonicModule } from '@ionic/angular';

@Component({
  selector: 'app-messages',
  templateUrl: './messages.page.html',
  styleUrls: ['./messages.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule, FormsModule],
})
export class MessagesPage implements OnInit {
  notifications: NotificationDto[] = [];
  expandedNotificationId: number | null = null;
  isLoading = true;

  constructor(
    private notificationService: NotificationService,
    private toastController: ToastController,
    private alertController: AlertController
  ) {}

  ngOnInit() {
    this.loadNotifications();
  }

  ionViewWillEnter() {
    this.loadNotifications();
  }

  // Load notifications from API
  loadNotifications() {
    this.isLoading = true; // Start loading
    this.notificationService.getNotifications().subscribe({
      next: (data) => {
        this.notifications = data;
        this.expandedNotificationId = null; // Ensure all accordions are collapsed
        this.isLoading = false; // Stop loading
      },
      error: (error) => {
        console.error('Error fetching notifications:', error);
        this.showToast('Failed to load notifications', 'danger');
        this.isLoading = false;
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
  deleteNotification(notification: NotificationDto) {
    this.notificationService.deleteNotification(notification.notificationId).subscribe({
      next: () => {
        this.notifications = this.notifications.filter(n => n.notificationId !== notification.notificationId);
        this.expandedNotificationId = null; // Collapse everything after deletion
        this.showToast('Notification deleted', 'success');
      },
      error: (error) => {
        console.error('Error deleting notification:', error);
        this.showToast('Failed to delete notification', 'danger');
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
