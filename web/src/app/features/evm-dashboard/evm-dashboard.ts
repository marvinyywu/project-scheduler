import { Component, inject } from '@angular/core';
import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { ScheduleStore } from '../../core/state/schedule-store.service';

@Component({
  selector: 'app-evm-dashboard',
  imports: [
    CurrencyPipe,
    DatePipe,
    DecimalPipe,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
  ],
  templateUrl: './evm-dashboard.html',
  styleUrl: './evm-dashboard.scss',
})
export class EvmDashboard {
  protected readonly store = inject(ScheduleStore);
  private readonly fb = inject(FormBuilder);

  protected readonly progressForm = this.fb.nonNullable.group({
    taskId: [null as number | null, Validators.required],
    percentComplete: [0, [Validators.required, Validators.min(0), Validators.max(100)]],
    actualCost: [0, [Validators.required, Validators.min(0)]],
  });

  protected async updateProgress(): Promise<void> {
    if (this.progressForm.invalid) {
      return;
    }

    const { taskId, percentComplete, actualCost } = this.progressForm.getRawValue();
    await this.store.updateTaskProgress(taskId!, { percentComplete, actualCost });
    this.progressForm.reset({ taskId: null, percentComplete: 0, actualCost: 0 });
  }

  protected async refreshEvm(asOfDay: number): Promise<void> {
    await this.store.loadEvm(asOfDay);
  }

  protected async captureBaseline(): Promise<void> {
    await this.store.captureBaseline();
  }
}
