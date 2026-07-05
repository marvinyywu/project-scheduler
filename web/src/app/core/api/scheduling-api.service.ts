import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { Project } from '../models/project';
import { ScheduleTask } from '../models/schedule-task';
import { Dependency } from '../models/dependency';
import { CreateProjectRequest } from '../models/create-project-request';
import { CreateTaskRequest } from '../models/create-task-request';
import { CreateDependencyRequest } from '../models/create-dependency-request';
import { RecomputeResponse } from '../models/recompute-response';
import { Resource } from '../models/resource';
import { CreateResourceRequest } from '../models/create-resource-request';
import { Assignment } from '../models/assignment';
import { CreateAssignmentRequest } from '../models/create-assignment-request';
import { LevelingResponse } from '../models/leveling-response';
import { UpdateTaskProgressRequest } from '../models/update-task-progress-request';
import { EvmReport } from '../models/evm-report';
import { Baseline } from '../models/baseline';
import { environment } from '../../../environments/environment';

const API_BASE_URL = environment.apiBaseUrl;

@Injectable({ providedIn: 'root' })
export class SchedulingApiService {
  private readonly http = inject(HttpClient);

  createProject(request: CreateProjectRequest): Promise<Project> {
    return firstValueFrom(this.http.post<Project>(`${API_BASE_URL}/projects`, request));
  }

  getProject(id: number): Promise<Project> {
    return firstValueFrom(this.http.get<Project>(`${API_BASE_URL}/projects/${id}`));
  }

  recompute(id: number): Promise<RecomputeResponse> {
    return firstValueFrom(this.http.post<RecomputeResponse>(`${API_BASE_URL}/projects/${id}/recompute`, {}));
  }

  addTask(projectId: number, request: CreateTaskRequest): Promise<ScheduleTask> {
    return firstValueFrom(this.http.post<ScheduleTask>(`${API_BASE_URL}/projects/${projectId}/tasks`, request));
  }

  getTasks(projectId: number): Promise<ScheduleTask[]> {
    return firstValueFrom(this.http.get<ScheduleTask[]>(`${API_BASE_URL}/projects/${projectId}/tasks`));
  }

  addDependency(request: CreateDependencyRequest): Promise<Dependency> {
    return firstValueFrom(this.http.post<Dependency>(`${API_BASE_URL}/dependencies`, request));
  }

  createResource(request: CreateResourceRequest): Promise<Resource> {
    return firstValueFrom(this.http.post<Resource>(`${API_BASE_URL}/resources`, request));
  }

  getResources(): Promise<Resource[]> {
    return firstValueFrom(this.http.get<Resource[]>(`${API_BASE_URL}/resources`));
  }

  addAssignment(request: CreateAssignmentRequest): Promise<Assignment> {
    return firstValueFrom(this.http.post<Assignment>(`${API_BASE_URL}/assignments`, request));
  }

  getAssignments(projectId: number): Promise<Assignment[]> {
    return firstValueFrom(this.http.get<Assignment[]>(`${API_BASE_URL}/projects/${projectId}/assignments`));
  }

  levelSchedule(projectId: number): Promise<LevelingResponse> {
    return firstValueFrom(this.http.post<LevelingResponse>(`${API_BASE_URL}/projects/${projectId}/level`, {}));
  }

  updateTaskProgress(
    projectId: number,
    taskId: number,
    request: UpdateTaskProgressRequest,
  ): Promise<ScheduleTask> {
    return firstValueFrom(
      this.http.patch<ScheduleTask>(`${API_BASE_URL}/projects/${projectId}/tasks/${taskId}/progress`, request),
    );
  }

  getEvmReport(projectId: number, asOfDay: number): Promise<EvmReport> {
    return firstValueFrom(
      this.http.get<EvmReport>(`${API_BASE_URL}/projects/${projectId}/evm`, { params: { asOfDay } }),
    );
  }

  captureBaseline(projectId: number): Promise<Baseline> {
    return firstValueFrom(this.http.post<Baseline>(`${API_BASE_URL}/projects/${projectId}/baseline`, {}));
  }

  async getBaseline(projectId: number): Promise<Baseline | null> {
    try {
      return await firstValueFrom(this.http.get<Baseline>(`${API_BASE_URL}/projects/${projectId}/baseline`));
    } catch (err) {
      if (err instanceof HttpErrorResponse && err.status === 404) {
        return null;
      }
      throw err;
    }
  }
}
