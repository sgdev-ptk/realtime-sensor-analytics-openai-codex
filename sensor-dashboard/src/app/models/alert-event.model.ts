export interface AlertEvent {
  id: string;
  sensorId: string;
  timestamp: string;
  value: number;
  message: string;
  severity: string;
}
