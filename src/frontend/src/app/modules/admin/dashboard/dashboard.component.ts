import { Component, ViewEncapsulation } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
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
        MatIconModule,
        MatButtonModule,
        NgApexchartsModule,
    ],
})
export class DashboardComponent {
    examCount = 12;
    submissionCount = 145;
    testCaseCount = 56;

    /** Mock average pass rate (% completed submissions treated as pass). */
    avgPassRate = 82;

    stats = [
        {
            title: 'Exams',
            value: String(this.examCount),
            subtitle: 'Configured',
            bg: 'bg-blue-50 dark:bg-blue-950/30',
            text: 'text-blue-600 dark:text-blue-400',
            icon: 'heroicons_outline:academic-cap',
            link: '/exams/data',
        },
        {
            title: 'Test cases',
            value: String(this.testCaseCount),
            subtitle: 'Across exams (mock)',
            bg: 'bg-sky-50 dark:bg-sky-950/30',
            text: 'text-sky-600 dark:text-sky-400',
            icon: 'heroicons_outline:queue-list',
            link: '/exams/data',
        },
        {
            title: 'Submissions',
            value: String(this.submissionCount),
            subtitle: 'All time',
            bg: 'bg-indigo-50 dark:bg-indigo-950/30',
            text: 'text-indigo-600 dark:text-indigo-400',
            icon: 'heroicons_outline:document-arrow-up',
            link: '/submissions/data',
        },
        {
            title: 'Avg. pass rate (mock)',
            value: `${this.avgPassRate}%`,
            subtitle: 'System estimate',
            bg: 'bg-emerald-50 dark:bg-emerald-950/30',
            text: 'text-emerald-600 dark:text-emerald-400',
            icon: 'heroicons_outline:chart-bar',
            link: '/submissions/data',
        },
    ];

    mainChartSeries: ApexAxisChartSeries = [
        { name: 'Submitted', data: [12, 18, 22, 28, 35, 41, 48] },
        { name: 'Pass', data: [10, 15, 18, 24, 30, 34, 40] },
    ];

    mainChartOptions: Partial<ChartOptions> = {
        chart: {
            height: 320,
            type: 'area',
            toolbar: { show: false },
            fontFamily: 'Inter, sans-serif',
        },
        dataLabels: { enabled: false },
        stroke: { curve: 'smooth', width: 2 },
        xaxis: {
            categories: ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'],
            axisBorder: { show: false },
            axisTicks: { show: false },
        },
        yaxis: { show: false },
        grid: {
            show: true,
            strokeDashArray: 4,
            padding: { left: 12, right: 12 },
        },
        colors: ['#2563EB', '#10B981'],
        fill: {
            type: 'gradient',
            gradient: {
                shadeIntensity: 1,
                opacityFrom: 0.55,
                opacityTo: 0.08,
                stops: [0, 90, 100],
            },
        },
        legend: {
            position: 'top',
            horizontalAlign: 'right',
        },
        tooltip: { y: { formatter: (val: number) => String(val) } },
    };

    outcomeChartSeries: number[] = [54, 12, 8, 3];
    outcomeChartOptions: Partial<ChartOptions> = {
        chart: {
            type: 'donut',
            height: 280,
            fontFamily: 'Inter, sans-serif',
        },
        labels: ['Pass', 'Fail', 'Skipped', 'Running / Error'],
        colors: ['#10B981', '#EF4444', '#EAB308', '#94A3B8'],
        legend: { position: 'bottom' },
        plotOptions: {
            pie: {
                donut: {
                    size: '72%',
                    labels: {
                        show: true,
                        name: { show: true },
                        value: { show: true },
                        total: {
                            show: true,
                            label: 'Total',
                            formatter: (w) =>
                                String(
                                    w.globals.seriesTotals.reduce(
                                        (a: number, b: number) => a + b,
                                        0
                                    )
                                ),
                        },
                    },
                },
            },
        },
        dataLabels: { enabled: false },
    };
}
