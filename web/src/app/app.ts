import { Component } from '@angular/core';
import { SchedulePage } from './features/schedule-page/schedule-page';

@Component({
  selector: 'app-root',
  imports: [SchedulePage],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {}
