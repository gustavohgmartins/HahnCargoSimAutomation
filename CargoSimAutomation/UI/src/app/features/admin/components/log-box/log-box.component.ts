import { Component, Input, AfterViewChecked, ElementRef, ViewChild, SimpleChanges, HostListener, ViewChildren, QueryList } from '@angular/core';

@Component({
  selector: 'app-log-box',
  templateUrl: './log-box.component.html',
  styleUrls: ['./log-box.component.css']
})
export class LogBoxComponent {
  @Input() entity!: string;
  @Input() logs!: any[];
  @Input() isRunning?: boolean;
  _logIcons: { [key: string]: string } = {
    "Loading": "pallet",
    "Picking": "pallet",
    // "Route": "route",
    // "Moving": "trending_flat",
    "Delivering": "local_shipping",
    "Order": "event_available",
  };

  @ViewChild('scrollBox') scrollBoxContainer!: ElementRef;
  @ViewChildren('messages') messages!: QueryList<any>;

  disableScrollDown: boolean = false;

  constructor() { }

  ngAfterViewInit() {
    this.scrollToBottom();
    this.messages.changes.subscribe(() => this.scrollToBottom());
  }

  scrollToBottom(): void {
    var scrollBox = this.scrollBoxContainer.nativeElement;
    try {
      scrollBox.scrollTop = scrollBox.scrollHeight;
    }
    catch (err) {
      console.log(err)
    }
  }

  getIcon(log: string) {
    var icon = this._logIcons[log.split(' ')[0]];
    return icon;
  }
}