export interface ScheduleTask {
  id: number;
  name: string;
  duration: number;
  projectId: number;
  earlyStart: number;
  earlyFinish: number;
  lateStart: number;
  lateFinish: number;
  totalFloat: number;
  freeFloat: number;
  isCritical: boolean;
  budget: number;
  percentComplete: number;
  actualCost: number;
}
