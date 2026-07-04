import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';

import { GanttChart } from './gantt-chart';

describe('GanttChart', () => {
  let component: GanttChart;
  let fixture: ComponentFixture<GanttChart>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GanttChart],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    fixture = TestBed.createComponent(GanttChart);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
