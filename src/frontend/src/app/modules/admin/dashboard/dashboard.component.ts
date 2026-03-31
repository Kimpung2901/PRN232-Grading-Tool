import { Component, OnInit, ViewEncapsulation } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ExamsService } from '../../../services/exams.service';
import { SubmissionsService } from '../../../services/submissions.service';
import { NgApexchartsModule } from 'ng-apexcharts';
import {
    ApexAxisChartSeries,
    ApexChart,
    ApexXAxis,
    ApexStroke,
    ApexTooltip,
    ApexDataLabels,
    ApexGrid,
    ApexLegend,
    ApexPlotOptions,
    ApexYAxis,
    ApexFill,
} from 'ng-apexcharts';

export type ChartOptions = {
    series: ApexAxisChartSeries;
    chart: ApexChart;
    xaxis: ApexXAxis;
    stroke: ApexStroke;
    tooltip: ApexTooltip;
    dataLabels: ApexDataLabels;
    grid: ApexGrid;
    legend: ApexLegend;
    plotOptions: ApexPlotOptions;
    yaxis: ApexYAxis;
    fill: ApexFill;
    labels: string[];
    colors: string[];
};

@Component({
    selector: 'dashboard',
    standalone: true,
    templateUrl: './dashboard.component.html',
    encapsulation: ViewEncapsulation.None,
    imports: [
        CommonModule,
        RouterModule,
        NgApexchartsModule,
    ],
})
export class DashboardComponent implements OnInit {
    examCount = 0;
    submissionCount = 0;
    testCaseCount = 0;
    avgPassRate = 82;

    isLoading = false;

    constructor(
        private examsService: ExamsService,
        private submissionsService: SubmissionsService
    ) {}

    ngOnInit(): void {
        this.loadStats();
    }

    loadStats(): void {
        this.isLoading = true;
        
        // Fetch exams count
        this.examsService.getExams({ page: 1, pageSize: 1 }).subscribe({
            next: (res) => {
                this.examCount = res.total || 0;
                this.updateStatValue('Exams', String(this.examCount));
                this.isLoading = false;
            },
            error: (err) => {
                this.isLoading = false;
                console.error('Failed to load exams stats', err);
            }
        });

        // The API lacks a global submission count, so for the dashboard, 
        // we'd ideally have a dedicated endpoint. 
        // For now, we fetch from a known exam or leave as mock for the UI demo.
        this.submissionsService.getSubmissions({ examId: 1, page: 1, pageSize: 1 }).subscribe({
            next: (res) => {
                this.submissionCount = res.total || 0;
                this.updateStatValue('Submissions', String(this.submissionCount));
            }
        });
    }

    private updateStatValue(title: string, value: string): void {
        const stat = this.stats.find(s => s.title === title);
        if (stat) {
            stat.value = value;
        }
    }

    stats = [
        {
            title: 'Exams',
            value: '0',
            subtitle: 'Active exams',
            bg: 'bg-indigo-50 dark:bg-indigo-900/20',
            text: 'text-indigo-600 dark:text-indigo-400',
            icon: 'academic-cap',
            link: '/exams',
        },
        {
            title: 'Submissions',
            value: '0',
            subtitle: 'Recent uploads',
            bg: 'bg-emerald-50 dark:bg-emerald-900/20',
            text: 'text-emerald-600 dark:text-emerald-400',
            icon: 'document-text',
            link: '/submissions',
        },
        {
            title: 'Test cases',
            value: '124',
            subtitle: 'System wide',
            bg: 'bg-amber-50 dark:bg-amber-900/20',
            text: 'text-amber-600 dark:text-amber-400',
            icon: 'beaker',
            link: '/exams',
        },
        {
            title: 'Pass Rate',
            value: '82%',
            subtitle: 'Avg. score',
            bg: 'bg-rose-50 dark:bg-rose-900/20',
            text: 'text-rose-600 dark:text-rose-400',
            icon: 'chart-bar',
            link: '/submissions',
        },
    ];

    mainChartSeries: ApexAxisChartSeries = [
        { name: 'Submissions', data: [31, 40, 28, 51, 42, 109, 100] },
        { name: 'Passed', data: [11, 32, 45, 32, 34, 52, 41] },
    ];

    mainChartOptions: Partial<ChartOptions> = {
        chart: {
            height: 350,
            type: 'area',
            toolbar: { show: false },
            fontFamily: 'inherit',
        },
        dataLabels: { enabled: false },
        stroke: { curve: 'smooth', width: 2 },
        xaxis: {
            categories: ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'],
        },
        colors: ['#6366F1', '#10B981'],
        fill: {
            type: 'gradient',
            gradient: {
                shadeIntensity: 1,
                opacityFrom: 0.45,
                opacityTo: 0.05,
                stops: [20, 100],
            },
        },
        grid: {
            borderColor: 'rgba(0,0,0,0.05)',
            strokeDashArray: 4,
        },
    };

    outcomeChartSeries: number[] = [65, 20, 10, 5];
    outcomeChartOptions: Partial<ChartOptions> = {
        chart: {
            type: 'donut',
            height: 300,
        },
        labels: ['Passed', 'Failed', 'Pending', 'Error'],
        colors: ['#10B981', '#EF4444', '#6366F1', '#F59E0B'],
        legend: { position: 'bottom' },
        plotOptions: {
            pie: {
                donut: {
                    size: '75%',
                    labels: {
                        show: true,
                        total: {
                            show: true,
                            label: 'Total',
                            formatter: (w) => '100'
                        }
                    }
                }
            }
        },
        dataLabels: { enabled: false },
    };
}
