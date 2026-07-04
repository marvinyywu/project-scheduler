import { DependencyType } from './dependency-type';

export interface CreateDependencyRequest {
  predecessorId: number;
  successorId: number;
  type: DependencyType;
  lagDays: number;
}
