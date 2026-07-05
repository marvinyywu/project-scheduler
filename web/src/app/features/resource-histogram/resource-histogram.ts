import { Component, computed, inject } from '@angular/core';
import { scaleLinear } from 'd3-scale';
import { ScheduleStore } from '../../core/state/schedule-store.service';

const CHART_WIDTH_PX = 800;
const ROW_HEIGHT_PX = 32;
const BAR_HEIGHT_PX = 20;

interface HistogramBar {
  resourceId: number;
  day: number;
  x: number;
  width: number;
  y: number;
  isOverAllocated: boolean;
  label: string;
}

@Component({
  selector: 'app-resource-histogram',
  imports: [],
  templateUrl: './resource-histogram.html',
  styleUrl: './resource-histogram.scss',
})
export class ResourceHistogram {
  protected readonly store = inject(ScheduleStore);

  protected readonly chartWidth = CHART_WIDTH_PX;
  protected readonly rowHeight = ROW_HEIGHT_PX;
  protected readonly barHeight = BAR_HEIGHT_PX;
  protected readonly chartHeight = computed(() => this.store.resources().length * ROW_HEIGHT_PX);

  protected readonly bars = computed<HistogramBar[]>(() => {
    const dayScale = scaleLinear().domain([0, this.store.projectDuration()]).range([0, CHART_WIDTH_PX]);
    const dayWidth = dayScale(1) - dayScale(0);
    const rows = this.store.resources();

    return this.store.histogram().map(usage => {
      const rowIndex = rows.findIndex(r => r.id === usage.resourceId);
      return {
        resourceId: usage.resourceId,
        day: usage.day,
        x: dayScale(usage.day),
        width: dayWidth,
        y: rowIndex * ROW_HEIGHT_PX + (ROW_HEIGHT_PX - BAR_HEIGHT_PX) / 2,
        isOverAllocated: usage.isOverAllocated,
        label: `${usage.usedUnits}/${usage.capacity}`,
      };
    });
  });
}
