import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToastController, AlertController, LoadingController } from '@ionic/angular';
import { UserService } from 'src/app/services/user.service';
import { ApplicationUser } from 'src/app/model/user-profile';
import { firstValueFrom } from 'rxjs';
import { FormsModule } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { TitleService } from 'src/app/services/title.service';
import { IonContent, IonSearchbar, IonList, IonItem, IonGrid, IonRow, IonCol, IonButton, IonSpinner } from '@ionic/angular/standalone';
import { BackendMessageService } from 'src/app/services/backend-message.service';

@Component({
  selector: 'app-user-manager',
  templateUrl: './user-manager.page.html',
  styleUrls: ['./user-manager.page.scss'],
  standalone: true,
  imports: [CommonModule, FormsModule, TranslateModule, IonContent, IonSearchbar, IonList, IonItem, IonGrid, IonRow, IonCol, IonButton, IonSpinner]
})
export class UserManagerPage implements OnInit {
  users: ApplicationUser[] = [];
  filteredUsers: ApplicationUser[] = [];
  searchTerm = '';
  isLoading = false;

  constructor(
    private userService: UserService,
    private toastController: ToastController,
    private alertController: AlertController,
    private loadingController: LoadingController,
    private titleService: TitleService,
    private translate: TranslateService,
    private backendMessages: BackendMessageService
  ) {}

  ngOnInit(): void {
    this.titleService.setTitle('USER_MANAGER.TITLE');
    this.loadUsers();
  }

  ionViewWillEnter(): void {
    this.titleService.setTitle('USER_MANAGER.TITLE');
    this.loadUsers();
  }
  
  async loadUsers() {
    this.isLoading = true;
  
    const loading = await this.loadingController.create({
      message: this.t('USER_MANAGER.LOADING_USERS'),
      spinner: 'crescent',
    });
    await loading.present();
  
    const startTime = Date.now();
  
    try {
      const result = await firstValueFrom(this.userService.getAllUsers());
      this.users = result ?? [];
      this.filteredUsers = [...this.users];
    } catch (error) {
      this.showToast(this.t('USER_MANAGER.LOAD_FAILED'), 'danger');
      console.error(error);
    } finally {
      const elapsedTime = Date.now() - startTime;
      const delay = Math.max(0, 800 - elapsedTime);
  
      setTimeout(async () => {
        await loading.dismiss();
        this.isLoading = false;
      }, delay);
    }
  } 

  filterUsers() {
    const query = this.searchTerm.toLowerCase();
    this.filteredUsers = this.users.filter(user =>
      user.userName.toLowerCase().includes(query) ||
      user.userEmail.toLowerCase().includes(query)
    );
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
                  this.filterUsers();
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
