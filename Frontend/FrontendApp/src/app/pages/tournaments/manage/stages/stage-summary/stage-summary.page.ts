import { Component, OnInit, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonicModule } from '@ionic/angular';
import { ReactiveFormsModule } from '@angular/forms';

@Component({
  selector: 'app-stage-summary',
  templateUrl: './stage-summary.page.html',
  styleUrls: ['./stage-summary.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule]
})
export class StageSummaryPage implements OnInit {
  @Input() tournamentName!: string;
  @Input() teamsArray!: { teamId: number | null; teamName: string }[];
  @Input() matchesArray!: any[];
  @Input() usersArray!: { userId: number; userName: string; userEmail: string; status: string }[];

  constructor() {}

  ngOnInit(): void {}
}
