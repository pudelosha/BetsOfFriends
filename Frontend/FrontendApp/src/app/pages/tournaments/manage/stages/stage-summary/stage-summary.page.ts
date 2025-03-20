import { Component, OnInit, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonicModule } from '@ionic/angular';
import { ReactiveFormsModule } from '@angular/forms';
import { Match, Stage, Team } from 'src/app/model/tournament-model';

@Component({
  selector: 'app-stage-summary',
  templateUrl: './stage-summary.page.html',
  styleUrls: ['./stage-summary.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule]
})
export class StageSummaryPage implements OnInit {
  @Input() tournamentName!: string;
  @Input() teamsArray!: Team[];
  @Input() matchesArray!: Match[];
  @Input() usersArray!: { userId: number; userName: string; userEmail: string; status: string }[];

  constructor() {}

  ngOnInit(): void {}

  getRecordStatusClass(recordStatus: string | null): string {
    switch (recordStatus) {
      case 'New': return 'status-new';
      case 'Update': return 'status-update';
      case 'Delete': return 'status-delete';
      case 'Uploaded': return 'status-uploaded';
      default: return '';
    }
  }  
}
