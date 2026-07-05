import { ScheduleTask } from '../models/schedule-task';
import { Assignment } from '../models/assignment';
import { Resource } from '../models/resource';
import { LeveledTask } from '../models/leveled-task';

export interface ResourceDayUsage {
  resourceId: number;
  day: number;
  usedUnits: number;
  capacity: number;
  isOverAllocated: boolean;
}

export function buildResourceHistogram(
  tasks: ScheduleTask[],
  assignments: Assignment[],
  resources: Resource[],
  leveledTasks: LeveledTask[] = [],
): ResourceDayUsage[] {
  const tasksById = new Map(tasks.map(t => [t.id, t]));
  const resourcesById = new Map(resources.map(r => [r.id, r]));
  const leveledById = new Map(leveledTasks.map(t => [t.taskId, t]));
  const usage = new Map<string, number>();

  // Once leveling has run, its dates take priority over the CPM early-start
  // window - the whole point of leveling is to show a different (later)
  // occupancy for the tasks it delayed.
  for (const assignment of assignments) {
    const task = tasksById.get(assignment.taskId);
    if (!task) continue;
    const leveled = leveledById.get(task.id);
    const start = leveled?.leveledStart ?? task.earlyStart;
    const finish = leveled?.leveledFinish ?? task.earlyFinish;
    for (let day = start; day < finish; day++) {
      const key = `${assignment.resourceId}:${day}`;
      usage.set(key, (usage.get(key) ?? 0) + assignment.units);
    }
  }

  return Array.from(usage.entries()).map(([key, usedUnits]) => {
    const [resourceIdStr, dayStr] = key.split(':');
    const resourceId = Number(resourceIdStr);
    const capacity = resourcesById.get(resourceId)?.maxUnitsPerDay ?? 0;
    return { resourceId, day: Number(dayStr), usedUnits, capacity, isOverAllocated: usedUnits > capacity };
  });
}
