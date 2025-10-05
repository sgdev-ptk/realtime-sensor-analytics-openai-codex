import { Injectable, OnDestroy } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, forkJoin } from 'rxjs';
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel
} from '@microsoft/signalr';
import { environment } from '../../environments/environment';
import { SensorReading } from '../models/sensor-reading.model';
import { SensorAggregate } from '../models/sensor-aggregate.model';
import { AlertEvent } from '../models/alert-event.model';
import { SensorBatch } from '../models/sensor-batch.model';

interface ChartBucket {
  key: string;
  timestamp: Date;
  sum: number;
  count: number;
}

@Injectable({ providedIn: 'root' })
export class SensorDataService implements OnDestroy {
  private readonly maxRecentReadings = 500;
  private readonly maxAlerts = 50;
  private readonly maxChartBuckets = 180;

  private hubConnection?: HubConnection;

  private readonly readingsSubject = new BehaviorSubject<SensorReading[]>([]);
  private readonly statsSubject = new BehaviorSubject<SensorAggregate | null>(null);
  private readonly alertsSubject = new BehaviorSubject<AlertEvent[]>([]);

  private readonly chartBuckets = new Map<string, ChartBucket>();

  readonly readings$ = this.readingsSubject.asObservable();
  readonly stats$ = this.statsSubject.asObservable();
  readonly alerts$ = this.alertsSubject.asObservable();

  constructor(private readonly http: HttpClient) {}

  connect(): void {
    if (this.hubConnection && this.hubConnection.state !== HubConnectionState.Disconnected) {
      return;
    }

    this.buildHubConnection();
    this.registerHandlers();
    this.startConnection();
    this.loadInitialData();
  }

  disconnect(): void {
    if (this.hubConnection) {
      void this.hubConnection.stop();
      this.hubConnection = undefined;
    }
  }

  ngOnDestroy(): void {
    this.disconnect();
  }

  get chartData() {
    const sorted = Array.from(this.chartBuckets.values()).sort(
      (a, b) => a.timestamp.getTime() - b.timestamp.getTime()
    );
    return {
      labels: sorted.map((bucket) => bucket.timestamp.toLocaleTimeString()),
      datasets: [
        {
          data: sorted.map((bucket) => bucket.sum / Math.max(bucket.count, 1)),
          label: 'Average Value',
          fill: false,
          tension: 0.2,
          borderColor: '#3b82f6',
          backgroundColor: 'rgba(59, 130, 246, 0.2)',
          pointRadius: 0
        }
      ]
    };
  }

  private buildHubConnection(): void {
    this.hubConnection = new HubConnectionBuilder()
      .withUrl(`${environment.apiBaseUrl}/hubs/sensors`)
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(LogLevel.Information)
      .build();
  }

  private registerHandlers(): void {
    if (!this.hubConnection) {
      return;
    }

    this.hubConnection.on('ReceiveSensorBatch', (payload: SensorBatch) => {
      this.processBatch(payload);
    });
  }

  private startConnection(): void {
    if (!this.hubConnection) {
      return;
    }

    void this.hubConnection
      .start()
      .catch((error) => console.error('SignalR connection error', error));
  }

  private loadInitialData(): void {
    const stats$ = this.http.get<SensorAggregate>(`${environment.apiBaseUrl}/api/readings/stats`);
    const alerts$ = this.http.get<AlertEvent[]>(`${environment.apiBaseUrl}/api/readings/alerts`, {
      params: { count: this.maxAlerts.toString() }
    });
    const recent$ = this.http.get<SensorReading[]>(`${environment.apiBaseUrl}/api/readings/recent`, {
      params: { count: this.maxRecentReadings.toString() }
    });

    forkJoin({ stats: stats$, alerts: alerts$, readings: recent$ }).subscribe({
      next: ({ stats, alerts, readings }) => {
        this.statsSubject.next(stats);
        this.alertsSubject.next(alerts);
        this.readingsSubject.next(readings);
        this.updateChart(readings);
      },
      error: (error) => console.error('Failed to load initial analytics data', error)
    });
  }

  private processBatch(batch: SensorBatch): void {
    if (batch.aggregate) {
      this.statsSubject.next(batch.aggregate);
    }

    if (Array.isArray(batch.readings) && batch.readings.length > 0) {
      const mergedReadings = [...this.readingsSubject.value, ...batch.readings];
      const trimmedReadings = mergedReadings.slice(-this.maxRecentReadings);
      this.readingsSubject.next(trimmedReadings);
      this.updateChart(batch.readings);
    }

    if (Array.isArray(batch.alerts) && batch.alerts.length > 0) {
      const mergedAlerts = [...batch.alerts, ...this.alertsSubject.value].slice(0, this.maxAlerts);
      this.alertsSubject.next(mergedAlerts);
    }
  }

  private updateChart(readings: SensorReading[]): void {
    readings.forEach((reading) => {
      const timestamp = new Date(reading.timestamp);
      const key = timestamp.toISOString().substring(0, 19);
      let bucket = this.chartBuckets.get(key);
      if (!bucket) {
        bucket = { key, timestamp, sum: 0, count: 0 };
        this.chartBuckets.set(key, bucket);
      }

      bucket.sum += reading.value;
      bucket.count += 1;
    });

    if (this.chartBuckets.size > this.maxChartBuckets) {
      const sortedKeys = Array.from(this.chartBuckets.keys()).sort();
      while (this.chartBuckets.size > this.maxChartBuckets) {
        const oldestKey = sortedKeys.shift();
        if (!oldestKey) {
          break;
        }
        this.chartBuckets.delete(oldestKey);
      }
    }
  }
}
