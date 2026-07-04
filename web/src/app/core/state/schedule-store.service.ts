import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { SchedulingApiService } from '../api/scheduling-api.service';
import { Project } from '../models/project';
import { ScheduleTask } from '../models/schedule-task';
import { CreateTaskRequest } from '../models/create-task-request';
import { CreateDependencyRequest } from '../models/create-dependency-request';

function isHttpConflict(error: unknown): boolean {
  return error instanceof HttpErrorResponse && error.status === 409;
}

@Injectable({ providedIn: 'root' })
export class ScheduleStore {
  private readonly api = inject(SchedulingApiService);

  readonly project = signal<Project | null>(null);
  readonly tasks = signal<ScheduleTask[]>([]);
  readonly error = signal<string | null>(null);

  readonly criticalTaskIds = computed(
    () => new Set(this.tasks().filter(t => t.isCritical).map(t => t.id)),
  );
  readonly projectDuration = computed(
    () => this.tasks().reduce((max, t) => Math.max(max, t.earlyFinish), 0),
  );

  async loadProject(projectId: number): Promise<void> {
    this.project.set(await this.api.getProject(projectId));
    await this.refreshTasks(projectId);
  }

  async addTask(projectId: number, request: CreateTaskRequest): Promise<void> {
    await this.api.addTask(projectId, request);
    await this.refreshTasks(projectId);
  }

  async addDependency(request: CreateDependencyRequest): Promise<void> {
    this.error.set(null);
    try {
      await this.api.addDependency(request);
      await this.refreshTasks(this.project()!.id);
    } catch (err) {
      if (isHttpConflict(err)) {
        this.error.set('That dependency would create a cycle and was rejected.');
      } else {
        throw err;
      }
    }
  }

  private async refreshTasks(projectId: number): Promise<void> {
    this.tasks.set(await this.api.getTasks(projectId));
  }
}
