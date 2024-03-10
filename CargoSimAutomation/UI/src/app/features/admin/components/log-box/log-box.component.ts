import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-log-box',
  templateUrl: './log-box.component.html',
  styleUrls: ['./log-box.component.css']
})
export class LogBoxComponent {
  @Input() entity!: string;
  @Input() logs!: any[];
}
