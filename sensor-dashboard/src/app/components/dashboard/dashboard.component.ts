import { AsyncPipe, CommonModule, DatePipe, DecimalPipe, NgFor, NgIf } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { ChartConfiguration, ChartOptions } from 'chart.js';
import { NgChartsModule } from 'ng2-charts';
import { Subscription } from 'rxjs';
import { SensorDataService } from '../../services/sensor-data.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, AsyncPipe, NgChartsModule, NgIf, NgFor, DecimalPipe, DatePipe],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss']
})
export class DashboardComponent implements OnInit, OnDestroy {
  stats$ = this.sensorData.stats$;
  alerts$ = this.sensorData.alerts$;
  readings$ = this.sensorData.readings$;

  chartData: ChartConfiguration<'line'>['data'] = { datasets: [], labels: [] };
  chartOptions: ChartOptions<'line'> = {
    responsive: true,
    maintainAspectRatio: false,
    scales: {
      x: {
        ticks: {
          color: '#cbd5f5'
        },
        grid: {
          color: 'rgba(148, 163, 184, 0.1)'
        }
      },
      y: {
        ticks: {
          color: '#cbd5f5'
        },
        grid: {
          color: 'rgba(148, 163, 184, 0.1)'
        }
      }
    },
    plugins: {
      legend: {
        labels: {
          color: '#e2e8f0'
        }
      }
    }
  };

  private subscriptions = new Subscription();

  constructor(private readonly sensorData: SensorDataService) {}

  ngOnInit(): void {
    this.sensorData.connect();
    this.chartData = this.sensorData.chartData;
    this.subscriptions.add(
      this.sensorData.readings$.subscribe(() => {
        this.chartData = this.sensorData.chartData;
      })
    );
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
    this.sensorData.disconnect();
  }

  trackByAlert(_: number, alert: { id: string }): string {
    return alert.id;
  }

  trackByReading(_: number, reading: { id: string }): string {
    return reading.id;
  }

}
