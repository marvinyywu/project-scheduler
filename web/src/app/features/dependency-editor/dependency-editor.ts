import { Component, effect, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ScheduleStore } from '../../core/state/schedule-store.service';
import { DependencyType } from '../../core/models/dependency-type';

const DEPENDENCY_TYPES: DependencyType[] = [
  'FinishToStart',
  'StartToStart',
  'FinishToFinish',
  'StartToFinish',
];

@Component({
  selector: 'app-dependency-editor',
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatSelectModule,
    MatInputModule,
    MatButtonModule,
  ],
  templateUrl: './dependency-editor.html',
  styleUrl: './dependency-editor.scss',
})
export class DependencyEditor {
  protected readonly store = inject(ScheduleStore);
  protected readonly dependencyTypes = DEPENDENCY_TYPES;

  private readonly fb = inject(FormBuilder);
  private readonly snackBar = inject(MatSnackBar);

  protected readonly addDependencyForm = this.fb.nonNullable.group({
    predecessorId: [null as number | null, Validators.required],
    successorId: [null as number | null, Validators.required],
    type: ['FinishToStart' as DependencyType, Validators.required],
    lagDays: [0, [Validators.required, Validators.min(0)]],
  });

  constructor() {
    effect(() => {
      const error = this.store.error();
      if (error) {
        this.snackBar.open(error, 'Dismiss', { duration: 5000 });
      }
    });
  }

  protected async submit(): Promise<void> {
    if (this.addDependencyForm.invalid) {
      return;
    }

    const { predecessorId, successorId, type, lagDays } = this.addDependencyForm.getRawValue();
    await this.store.addDependency({
      predecessorId: predecessorId!,
      successorId: successorId!,
      type,
      lagDays,
    });
  }
}
