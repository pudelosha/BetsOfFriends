import { CommonModule } from '@angular/common';
import { Component, Input, Output, EventEmitter, AfterViewInit, ElementRef, ViewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-wheeler-number-picker',
  templateUrl: './wheeler-number-picker.component.html',
  styleUrls: ['./wheeler-number-picker.component.scss'],
  standalone: true,
  imports: [CommonModule, FormsModule],
})
export class WheelerNumberPickerComponent  implements AfterViewInit {
  @Input() selectedNumber: number = 0;
  @Output() numberChanged = new EventEmitter<number>();

  numberOptions = Array.from({ length: 10 }, (_, i) => i);
  selectedIndex = 0;
  
  @ViewChild('wheelContainer') wheelContainer!: ElementRef;

  constructor() {}

  ngAfterViewInit() {
    this.updateSelection();
  }

  selectNumber(index: number) {
    this.selectedIndex = index;
    this.selectedNumber = this.numberOptions[index];
    this.numberChanged.emit(this.selectedNumber);
  }

  updateSelection() {
    const index = this.numberOptions.indexOf(this.selectedNumber);
    if (index !== -1) {
      this.selectedIndex = index;
    }
  }
}
