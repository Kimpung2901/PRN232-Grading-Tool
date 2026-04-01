import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { SubmissionsService } from '../../../../services/submissions.service';
import { SubmissionReportDto, SubmissionTestResultDto } from '../../../../api/models';

// Angular Material
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialogModule } from '@angular/material/dialog';

@Component({
    selector: 'app-submissions-grading',
    standalone: true,
    imports: [
        CommonModule, RouterModule,
        MatTableModule, MatButtonModule, MatIconModule,
        MatProgressSpinnerModule, MatProgressBarModule,
        MatTooltipModule, MatSnackBarModule, MatChipsModule, MatDialogModule
    ],
    templateUrl: './grading.component.html'
})
export class GradingComponent implements OnInit {
    submissionId = 0;
    examId = 0;
    totalScore = 0;
    maxScore = 10.0;

    selectedLog: string | null = null;
    isLogOpen = false;

    results: SubmissionTestResultDto[] = [];
    isLoading = false;

    displayedColumns: string[] = ['testCaseName', 'status', 'point', 'log'];

    constructor(
        private route: ActivatedRoute,
        private submissionsService: SubmissionsService
    ) {
        const idParam = this.route.snapshot.paramMap.get('id');
        this.submissionId = idParam ? parseInt(idParam, 10) : 0;
    }

    ngOnInit(): void {
        this.route.queryParams.subscribe(params => {
            this.examId = params['examId'] ? parseInt(params['examId'], 10) : 0;
            if (this.submissionId && this.examId) {
                this.loadReport();
            } else if (this.submissionId) {
                this.loadSubmissionAndReport();
            }
        });
    }

    loadSubmissionAndReport(): void {
        this.isLoading = true;
        this.submissionsService.getSubmissionById({ id: this.submissionId }).subscribe({
            next: (s) => {
                this.examId = s.examId || 0;
                this.loadReport();
            },
            error: () => {
                this.isLoading = false;
            }
        });
    }

    loadReport(): void {
        if (!this.examId || !this.submissionId) return;
        this.isLoading = true;
        this.submissionsService.getSubmissionReports({ examId: this.examId }).subscribe({
            next: (reports) => {
                const report = reports.find(r => r.submissionId === this.submissionId);
                if (report) {
                    this.totalScore = report.totalScore || 0;
                    this.results = report.results || [];
                }
                this.isLoading = false;
            },
            error: () => {
                this.isLoading = false;
            }
        });
    }

    get scorePercent(): number {
        return this.maxScore > 0 ? (this.totalScore / this.maxScore) * 100 : 0;
    }

    getStatusClass(outcome: string | undefined | null): string {
        switch (outcome) {
            case 'Passed': return 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-400';
            case 'Failed': return 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400';
            case 'Skipped': return 'bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400';
            default: return 'bg-gray-100 text-gray-700 dark:bg-gray-800 dark:text-gray-400';
        }
    }

    viewLog(log: string | undefined | null): void {
        if (!log) return;
        this.selectedLog = log;
        this.isLogOpen = true;
    }

    closeLog(): void {
        this.isLogOpen = false;
        setTimeout(() => { this.selectedLog = null; }, 300);
    }

    get passedCount(): number {
        return this.results.filter(r => r.outcome === 'Passed').length;
    }

    get failedCount(): number {
        return this.results.filter(r => r.outcome === 'Failed').length;
    }
}
