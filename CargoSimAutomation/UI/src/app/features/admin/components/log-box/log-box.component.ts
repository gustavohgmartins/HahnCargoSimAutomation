import { Component, Input, AfterViewChecked, ElementRef, ViewChild } from '@angular/core';

@Component({
  selector: 'app-log-box',
  templateUrl: './log-box.component.html',
  styleUrls: ['./log-box.component.css']
})
export class LogBoxComponent implements AfterViewChecked {
  @Input() entity!: string;
  @Input() logs!: any[];
  @Input() isRunning?: boolean;

  @ViewChild('scrollBox') private scrollBoxContainer!: ElementRef;

  constructor() { }

  ngAfterViewChecked() {
    this.scrollToBottom();
  }

  scrollToBottom(): void {
    try {
      this.scrollBoxContainer.nativeElement.scrollTop = this.scrollBoxContainer.nativeElement.scrollHeight;
    } catch(err) { }
  }
}