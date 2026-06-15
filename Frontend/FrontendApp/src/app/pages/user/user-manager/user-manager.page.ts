import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToastController, AlertController, LoadingController, ModalController } from '@ionic/angular';
import { UserService } from 'src/app/services/user.service';
import { ApplicationUser } from 'src/app/model/user-profile';
import { firstValueFrom } from 'rxjs';
import { FormsModule } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { TitleService } from 'src/app/services/title.service';
import { IonContent, IonSearchbar, IonList, IonItem, IonGrid, IonRow, IonCol, IonButton, IonSpinner, IonIcon } from '@ionic/angular/standalone';
import { BackendMessageService } from 'src/app/services/backend-message.service';
import { MessageTextModalComponent } from 'src/app/modals/message-text-modal/message-text-modal.component';
import { NotificationService } from 'src/app/services/notification.service';

@Component({
  selector: 'app-user-manager',
  templateUrl: './user-manager.page.html',
  styleUrls: ['./user-manager.page.scss'],
  standalone: true,
  imports: [CommonModule, FormsModule, TranslateModule, IonContent, IonSearchbar, IonList, IonItem, IonGrid, IonRow, IonCol, IonButton, IonSpinner, IonIcon]
})
export class UserManagerPage implements OnInit {
  users: ApplicationUser[] = [];
  searchTerm = '';
  isLoading = false;
  showListSpinner = false;
  currentPage = 1;
  readonly pageSize = 20;
  totalPages = 1;
  totalCount = 0;

  constructor(
    private userService: UserService,
    private toastController: ToastController,
    private alertController: AlertController,
    private loadingController: LoadingController,
    private modalController: ModalController,
    private titleService: TitleService,
    private translate: TranslateService,
    private backendMessages: BackendMessageService,
    private notificationService: NotificationService
  ) {}

  ngOnInit(): void {
    this.titleService.setTitle('USER_MANAGER.TITLE');
    this.loadUsers();
  }

  ionViewWillEnter(): void {
    this.titleService.setTitle('USER_MANAGER.TITLE');
    this.loadUsers();
  }
  
  async loadUsers(page = this.currentPage, showOverlay = true) {
    this.isLoading = true;
    this.showListSpinner = !showOverlay;

    const loading = showOverlay
      ? await this.loadingController.create({
          message: this.t('USER_MANAGER.LOADING_USERS'),
          spinner: 'crescent',
        })
      : null;

    if (loading) {
      await loading.present();
    }
  
    const startTime = Date.now();
  
    try {
      const result = await firstValueFrom(this.userService.getUsersPage(page, this.pageSize, this.searchTerm));
      this.users = result?.items ?? [];
      this.currentPage = result?.page ?? page;
      this.totalPages = result?.totalPages ?? 1;
      this.totalCount = result?.totalCount ?? this.users.length;
    } catch (error) {
      this.showToast(this.t('USER_MANAGER.LOAD_FAILED'), 'danger');
      console.error(error);
    } finally {
      const elapsedTime = Date.now() - startTime;
      const delay = loading ? Math.max(0, 800 - elapsedTime) : 0;
  
      setTimeout(async () => {
        await loading?.dismiss();
        this.isLoading = false;
        this.showListSpinner = false;
      }, delay);
    }
  } 

  filterUsers() {
    this.currentPage = 1;
    this.loadUsers(1, false);
  }

  prevPage(): void {
    if (this.currentPage > 1) {
      this.loadUsers(this.currentPage - 1, false);
    }
  }

  nextPage(): void {
    if (this.currentPage < this.totalPages) {
      this.loadUsers(this.currentPage + 1, false);
    }
  }

  async toggleUserSuspension(user: ApplicationUser) {
    const isSuspended = user.userStatus === 'Suspended';
  
    try {
      const observable = isSuspended
        ? this.userService.unsuspendUser(user.userId)
        : this.userService.suspendUser(user.userId);
  
      const response = await firstValueFrom(observable);
  
      if (response?.success) {
        this.showToast(
          this.backendMessages.translateMessage(response.message, isSuspended ? 'USER_MANAGER.UNSUSPENDED' : 'USER_MANAGER.SUSPENDED', true),
          'success'
        );
        this.loadUsers();
      } else {
        this.showToast(
          this.backendMessages.translateMessage(response?.message, isSuspended ? 'USER_MANAGER.UNSUSPEND_FAILED' : 'USER_MANAGER.SUSPEND_FAILED'),
          'danger'
        );
      }
    } catch (error) {
      console.error(error);
      this.showToast(this.t('USER_MANAGER.ACTION_FAILED'), 'danger');
    }
  }
    
  async deleteUser(user: ApplicationUser) {
    const alert = await this.alertController.create({
      header: this.t('USER_MANAGER.CONFIRM_DELETE_TITLE'),
      message: this.t('USER_MANAGER.CONFIRM_DELETE_MESSAGE', { name: user.userName }),
      buttons: [
        { text: this.t('USER_MANAGER.CANCEL'), role: 'cancel' },
        {
          text: this.t('USER_MANAGER.DELETE'),
          handler: () => {
            this.userService.deleteUser(user.userId).subscribe({
              next: async (res) => {
                await this.showToast(
                  this.backendMessages.translateMessage(res.message, res.success ? 'USER_MANAGER.DELETED' : 'USER_MANAGER.DELETE_FAILED', res.success),
                  res.success ? 'success' : 'danger'
                );
  
                if (res.success) {
                  this.users = this.users.filter(u => u.userId !== user.userId);
                  this.loadUsers(this.currentPage, false);
                }
              },
              error: async (err) => {
                console.error('Error deleting user:', err);
                this.showToast(this.t('USER_MANAGER.DELETE_FAILED'), 'danger');
              }
            });
          }
        }
      ]
    });
  
    await alert.present();
  }

  async openBroadcastMessageModal(): Promise<void> {
    const modal = await this.modalController.create({
      component: MessageTextModalComponent,
      componentProps: {
        titleKey: 'USER_MANAGER.BROADCAST_TITLE',
        descriptionKey: 'USER_MANAGER.BROADCAST_DESCRIPTION',
        submitKey: 'USER_MANAGER.SEND_MESSAGE',
        includeEmailOption: true,
        emailOptionKey: 'USER_MANAGER.SEND_EMAIL_COPY'
      },
      breakpoints: [0, 0.65, 1],
      initialBreakpoint: 0.65,
    });

    await modal.present();
    const { data } = await modal.onWillDismiss();

    if (!data?.message) {
      return;
    }

    const loading = await this.loadingController.create({
      message: this.t('USER_MANAGER.SENDING_MESSAGE'),
      spinner: 'crescent',
    });
    await loading.present();

    this.notificationService.sendAdminBroadcastMessage({ message: data.message, sendEmail: !!data.sendEmail }).subscribe({
      next: async (response) => {
        await loading.dismiss();
        await this.showToast(
          this.t('USER_MANAGER.BROADCAST_SENT', { count: response.recipientCount }),
          'success'
        );
      },
      error: async (error) => {
        await loading.dismiss();
        console.error('Error sending broadcast message:', error);
        await this.showToast(this.t('USER_MANAGER.BROADCAST_FAILED'), 'danger');
      }
    });
  }

  async openUserMessageModal(user: ApplicationUser): Promise<void> {
    const recipientName = user.userName || user.userEmail;
    const modal = await this.modalController.create({
      component: MessageTextModalComponent,
      componentProps: {
        titleKey: 'USER_MANAGER.DIRECT_MESSAGE_TITLE',
        titleParams: { name: recipientName },
        descriptionKey: 'USER_MANAGER.DIRECT_MESSAGE_DESCRIPTION',
        descriptionParams: { name: recipientName },
        submitKey: 'USER_MANAGER.SEND_MESSAGE',
        includeEmailOption: true,
        emailOptionKey: 'USER_MANAGER.SEND_EMAIL_COPY'
      },
      breakpoints: [0, 0.65, 1],
      initialBreakpoint: 0.65,
    });

    await modal.present();
    const { data } = await modal.onWillDismiss();

    if (!data?.message) {
      return;
    }

    const loading = await this.loadingController.create({
      message: this.t('USER_MANAGER.SENDING_MESSAGE'),
      spinner: 'crescent',
    });
    await loading.present();

    this.notificationService.sendAdminUserMessage({
      recipientUserId: user.userId,
      message: data.message,
      sendEmail: !!data.sendEmail
    }).subscribe({
      next: async () => {
        await loading.dismiss();
        await this.showToast(this.t('USER_MANAGER.DIRECT_MESSAGE_SENT'), 'success');
      },
      error: async (error) => {
        await loading.dismiss();
        console.error('Error sending user message:', error);
        const message = error?.error?.message || error?.error?.Message || this.t('USER_MANAGER.BROADCAST_FAILED');
        await this.showToast(message, 'danger');
      }
    });
  }
  
  async showToast(message: string, color: 'success' | 'danger' | 'warning') {
    const toast = await this.toastController.create({
      message,
      duration: 3000,
      position: 'bottom',
      color
    });
    await toast.present();
  }

  private t(key: string, params?: Record<string, unknown>): string {
    return this.translate.instant(key, params);
  }
}
