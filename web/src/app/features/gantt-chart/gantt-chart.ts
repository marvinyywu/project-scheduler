import { Component, computed, inject } from '@angular/core';
import { scaleLinear } from 'd3-scale';
import { ScheduleStore } from '../../core/state/schedule-store.service';

const CHART_WIDTH_PX = 800;
const ROW_HEIGHT_PX = 32;
const BAR_HEIGHT_PX = 20;
const BASELINE_BAR_HEIGHT_PX = 6;

interface GanttBar {
  taskId: number;
  x: number;
  y: number;
  width: number;
  isCritical: boolean;
}

interface BaselineBar {
  taskId: number;
  x: number;
  y: number;
  width: number;
}

@Component({
  selector: 'app-gantt-chart',
  imports: [],
  templateUrl: './gantt-chart.html',
  styleUrl: './gantt-chart.scss',
})
export class GanttChart {
  protected readonly store = inject(ScheduleStore);

  protected readonly chartWidth = CHART_WIDTH_PX;
  protected readonly rowHeight = ROW_HEIGHT_PX;
  protected readonly barHeight = BAR_HEIGHT_PX;
  protected readonly baselineBarHeight = BASELINE_BAR_HEIGHT_PX;
  protected readonly chartHeight = computed(() => this.store.tasks().length * ROW_HEIGHT_PX);

  protected readonly bars = computed<GanttBar[]>(() => {
    const dayScale = scaleLinear().domain([0, this.store.projectDuration()]).range([0, CHART_WIDTH_PX]);
    const criticalTaskIds = this.store.criticalTaskIds();

    return this.store.tasks().map((task, index) => ({
      taskId: task.id,
      x: dayScale(task.earlyStart),
      y: index * ROW_HEIGHT_PX + (ROW_HEIGHT_PX - BAR_HEIGHT_PX) / 2,
      width: dayScale(task.duration) - dayScale(0),
      isCritical: criticalTaskIds.has(task.id),
    }));
  });

  protected readonly baselineBars = computed<BaselineBar[]>(() => {
    const baseline = this.store.baseline();
    if (!baseline) {
      return [];
    }

    const dayScale = scaleLinear().domain([0, this.store.projectDuration()]).range([0, CHART_WIDTH_PX]);
    const snapshotByTaskId = new Map(baseline.tasks.map(t => [t.taskId, t]));

    return this.store
      .tasks()
      .map((task, index) => {
        const snapshot = snapshotByTaskId.get(task.id);
        if (!snapshot) {
          return null;
        }

        return {
          taskId: task.id,
          x: dayScale(snapshot.earlyStart),
          y: index * ROW_HEIGHT_PX + (ROW_HEIGHT_PX - BAR_HEIGHT_PX) / 2 + BAR_HEIGHT_PX * 0.6,
          width: dayScale(snapshot.earlyFinish) - dayScale(snapshot.earlyStart),
        };
      })
      .filter((bar): bar is BaselineBar => bar !== null);
  });
}
