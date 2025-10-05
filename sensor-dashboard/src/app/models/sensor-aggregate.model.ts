export interface SensorAggregate {
  totalReadings: number;
  averageValue: number;
  minValue: number;
  maxValue: number;
  averageTemperature: number;
  averageHumidity: number;
  standardDeviation: number;
  windowStart: string;
  windowEnd: string;
}
