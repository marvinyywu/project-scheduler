import { Component, OnInit, inject } from '@angular/core';
import { ScheduleStore } from '../../core/state/schedule-store.service';
import { TaskList } from '../task-list/task-list';
import { DependencyEditor } from '../dependency-editor/dependency-editor';
import { GanttChart } from '../gantt-chart/gantt-chart';
import { ResourcePanel } from '../resource-panel/resource-panel';
import { ResourceHistogram } from '../resource-histogram/resource-histogram';
import { EvmDashboard } from '../evm-dashboard/evm-dashboard';

const PROJECT_ID = 1;

@Component({
  selector: 'app-schedule-page',
  imports: [TaskList, DependencyEditor, GanttChart, ResourcePanel, ResourceHistogram, EvmDashboard],
  templateUrl: './schedule-page.html',
  styleUrl: './schedule-page.scss',
})
export class SchedulePage implements OnInit {
  protected readonly store = inject(ScheduleStore);

  ngOnInit(): void {
    this.store.loadProject(PROJECT_ID);
  }
}
