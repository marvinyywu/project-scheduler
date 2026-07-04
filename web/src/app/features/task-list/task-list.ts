import { Component, inject } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { ScheduleStore } from '../../core/state/schedule-store.service';

function integerValidator(control: AbstractControl): ValidationErrors | null {
  return Number.isInteger(control.value) ? null : { notInteger: true };
}

@Component({
  selector: 'app-task-list',
  imports: [
    ReactiveFormsModule,
    MatTableModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
  ],
  templateUrl: './task-list.html',
  styleUrl: './task-list.scss',
})
export class TaskList {
  protected readonly store = inject(ScheduleStore);

  protected readonly displayedColumns = [
    'name',
    'duration',
    'earlyStart',
    'earlyFinish',
    'lateStart',
    'lateFinish',
    'totalFloat',
    'isCritical',
  ];

  private readonly fb = inject(FormBuilder);

  protected readonly addTaskForm = this.fb.nonNullable.group({
    name: ['', Validators.required],
    duration: [1, [Validators.required, Validators.min(1), integerValidator]],
  });

  protected async submit(): Promise<void> {
    const projectId = this.store.project()?.id;
    if (this.addTaskForm.invalid || projectId === undefined) {
      return;
    }

    await this.store.addTask(projectId, this.addTaskForm.getRawValue());
    this.addTaskForm.reset({ name: '', duration: 1 });
  }
}
