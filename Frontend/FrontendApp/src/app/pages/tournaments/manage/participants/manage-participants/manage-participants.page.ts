import { Component, CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { IonContent,  } from '@ionic/angular/standalone';
import { UserListPage } from '../user-list/user-list.page';
import { PendingRequestsPage } from '../pending-requests/pending-requests.page';
import { PendingInvitesPage } from '../pending-invites/pending-invites.page';

@Component({
  selector: 'app-manage-participants',
  templateUrl: './manage-participants.page.html',
  styleUrls: ['./manage-participants.page.scss'],
  standalone: true,
  imports: [IonContent, CommonModule, FormsModule, UserListPage, PendingRequestsPage, PendingInvitesPage],
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
})
export class ManageParticipantsPage  {

  constructor() { }

}
