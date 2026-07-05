import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { ScheduleStore } from '../../core/state/schedule-store.service';

@Component({
  selector: 'app-resource-panel',
  imports: [ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatButtonModule],
  templateUrl: './resource-panel.html',
  styleUrl: './resource-panel.scss',
})
export class ResourcePanel {
  protected readonly store = inject(ScheduleStore);
  private readonly fb = inject(FormBuilder);

  protected readonly addResourceForm = this.fb.nonNullable.group({
    name: ['', Validators.required],
    maxUnitsPerDay: [1, [Validators.required, Validators.min(1)]],
  });

  protected readonly assignResourceForm = this.fb.nonNullable.group({
    taskId: [null as number | null, Validators.required],
    resourceId: [null as number | null, Validators.required],
    units: [1, [Validators.required, Validators.min(1)]],
  });

  protected async submitResource(): Promise<void> {
    if (this.addResourceForm.invalid) {
      return;
    }

    await this.store.addResource(this.addResourceForm.getRawValue());
    this.addResourceForm.reset({ name: '', maxUnitsPerDay: 1 });
  }

  protected async submitAssignment(): Promise<void> {
    if (this.assignResourceForm.invalid) {
      return;
    }

    const { taskId, resourceId, units } = this.assignResourceForm.getRawValue();
    await this.store.assignResource({ taskId: taskId!, resourceId: resourceId!, units });
    this.assignResourceForm.reset({ taskId: null, resourceId: null, units: 1 });
  }

  protected async level(): Promise<void> {
    await this.store.levelSchedule();
  }
}
