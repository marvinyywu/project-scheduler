import { DependencyType } from './dependency-type';

export interface Dependency {
  id: number;
  predecessorId: number;
  successorId: number;
  type: DependencyType;
  lagDays: number;
}
