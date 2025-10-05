import { AlertEvent } from './alert-event.model';
import { SensorAggregate } from './sensor-aggregate.model';
import { SensorReading } from './sensor-reading.model';

export interface SensorBatch {
  readings: SensorReading[];
  aggregate: SensorAggregate;
  alerts: AlertEvent[];
}
