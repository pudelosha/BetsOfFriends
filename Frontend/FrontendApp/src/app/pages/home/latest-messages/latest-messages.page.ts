import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-latest-messages',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './latest-messages.page.html',
  styleUrls: ['./latest-messages.page.scss']
})
export class LatestMessagesPage {
  messages = [
    { sender: 'Alice', content: 'Hello!', timestamp: '10:00 AM' },
    { sender: 'Bob', content: 'How are you?', timestamp: '10:05 AM' },
    { sender: 'Charlie', content: 'Meeting at 3?', timestamp: '10:10 AM' },
    { sender: 'David', content: 'Sure!', timestamp: '10:15 AM' },
    { sender: 'Eve', content: 'See you then.', timestamp: '10:20 AM' }
  ];

  constructor() {
    console.log('Messages:', this.messages); // Debugging: Ensure data exists
  }
}

