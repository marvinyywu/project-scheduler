import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { SchedulingApiService } from '../api/scheduling-api.service';
import { Project } from '../models/project';
import { ScheduleTask } from '../models/schedule-task';
import { CreateTaskRequest } from '../models/create-task-request';
import { CreateDependencyRequest } from '../models/create-dependency-request';
import { Resource } from '../models/resource';
import { CreateResourceRequest } from '../models/create-resource-request';
import { Assignment } from '../models/assignment';
import { CreateAssignmentRequest } from '../models/create-assignment-request';
import { LevelingResponse } from '../models/leveling-response';
import { buildResourceHistogram } from './resource-histogram';

function isHttpConflict(error: unknown): boolean {
  return error instanceof HttpErrorResponse && error.status === 409;
}

@Injectable({ providedIn: 'root' })
export class ScheduleStore {
  private readonly api = inject(SchedulingApiService);

  readonly project = signal<Project | null>(null);
  readonly tasks = signal<ScheduleTask[]>([]);
  readonly error = signal<string | null>(null);
  readonly resources = signal<Resource[]>([]);
  readonly assignments = signal<Assignment[]>([]);
  readonly leveling = signal<LevelingResponse | null>(null);

  readonly criticalTaskIds = computed(
    () => new Set(this.tasks().filter(t => t.isCritical).map(t => t.id)),
  );
  readonly projectDuration = computed(
    () => this.tasks().reduce((max, t) => Math.max(max, t.earlyFinish), 0),
  );
  readonly histogram = computed(
    () => buildResourceHistogram(this.tasks(), this.assignments(), this.resources(), this.leveling()?.tasks ?? []),
  );

  async loadProject(projectId: number): Promise<void> {
    this.project.set(await this.api.getProject(projectId));
    await this.refreshTasks(projectId);
    this.resources.set(await this.api.getResources());
    await this.refreshAssignments(projectId);
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

  async addResource(request: CreateResourceRequest): Promise<void> {
    const resource = await this.api.createResource(request);
    this.resources.update(resources => [...resources, resource]);
  }

  async assignResource(request: CreateAssignmentRequest): Promise<void> {
    await this.api.addAssignment(request);
    await this.refreshAssignments(this.project()!.id);
  }

  async levelSchedule(): Promise<void> {
    this.leveling.set(await this.api.levelSchedule(this.project()!.id));
  }

  private async refreshTasks(projectId: number): Promise<void> {
    this.tasks.set(await this.api.getTasks(projectId));
  }

  private async refreshAssignments(projectId: number): Promise<void> {
    this.assignments.set(await this.api.getAssignments(projectId));
  }
}
