import { BaselineTaskSnapshot } from './baseline-task-snapshot';

export interface Baseline {
  capturedAt: string;
  tasks: BaselineTaskSnapshot[];
}
