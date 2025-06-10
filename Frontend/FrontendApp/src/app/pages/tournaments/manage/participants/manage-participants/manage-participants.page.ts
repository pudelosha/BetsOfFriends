import { Component, CUSTOM_ELEMENTS_SCHEMA, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { UserListPage } from '../user-list/user-list.page';
import { PendingRequestsPage } from '../pending-requests/pending-requests.page';
import { PendingInvitesPage } from '../pending-invites/pending-invites.page';
import { TranslateModule } from '@ngx-translate/core';
import { TitleService } from 'src/app/services/title.service';
import { IonContent } from '@ionic/angular/standalone';

@Component({
  selector: 'app-manage-participants',
  templateUrl: './manage-participants.page.html',
  styleUrls: ['./manage-participants.page.scss'],
  standalone: true,
  imports: [IonContent, CommonModule, FormsModule, UserListPage, PendingRequestsPage, PendingInvitesPage, TranslateModule],
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
})
export class ManageParticipantsPage implements OnInit {
  refreshCounter = 0;

  triggerRefresh() {
    this.refreshCounter++;
  }

  ngOnInit() {
    this.titleService.setTitle('MANAGE_PARTICIPANTS.TITLE');
  }

  ionViewWillEnter(){
    this.titleService.setTitle('MANAGE_PARTICIPANTS.TITLE');
    this.triggerRefresh();
  }

  constructor(private titleService: TitleService) { }
}