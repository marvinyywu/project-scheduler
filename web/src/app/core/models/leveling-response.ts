import { LeveledTask } from './leveled-task';

export interface LevelingResponse {
  originalProjectDuration: number;
  leveledProjectDuration: number;
  tasks: LeveledTask[];
}
