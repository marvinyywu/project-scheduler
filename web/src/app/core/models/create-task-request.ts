export interface CreateTaskRequest {
  name: string;
  duration: number;
  budget?: number;
}
