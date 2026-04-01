import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { SignalRService, RunnerStatus, RunnerCompletionLog } from '../../../../services/signalr.service';
import { TestResultsService } from '../../../../services/test-results.service';
import { ExamsService } from '../../../../services/exams.service';
import { ExamDto, TestResultRequest } from '../../../../api/models';
import { Subscription } from 'rxjs';

// Angular Material
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { MatChipsModule } from '@angular/material/chips';
import { MatBadgeModule } from '@angular/material/badge';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDividerModule } from '@angular/material/divider';

@Component({
    selector: 'app-grading-runner',
    standalone: true,
    imports: [
        CommonModule, FormsModule, RouterModule,
        MatSelectModule, MatFormFieldModule, MatButtonModule,
        MatIconModule, MatProgressSpinnerModule, MatSnackBarModule,
        MatTableModule, MatChipsModule, MatBadgeModule,
        MatTooltipModule, MatDividerModule
    ],
    templateUrl: './runner.component.html'
})
export class RunnerComponent implements OnInit, OnDestroy {
    status: RunnerStatus = { isRunning: false };
    results: TestResultRequest[] = [];
    exams: ExamDto[] = [];
    selectedExamId: number | null = null;

    lastLog: RunnerCompletionLog | null = null;
    isProcessing = false;
    errorMessage: string | null = null;

    displayedColumns: string[] = ['studentName', 'score', 'status'];

    private subs = new Subscription();

    constructor(
        private signalRService: SignalRService,
        private testResultsService: TestResultsService,
        private examsService: ExamsService,
        private snackBar: MatSnackBar
    ) {}

    ngOnInit(): void {
        this.signalRService.startConnection();

        this.subs.add(this.signalRService.status$.subscribe(s => {
            this.status = s;
            if (s.isRunning) {
                this.errorMessage = null;
            }
        }));

        this.subs.add(this.signalRService.testResult$.subscribe(r => {
            this.results.unshift(r);
            if (this.results.length > 50) this.results.pop();
        }));

        this.subs.add(this.signalRService.completion$.subscribe(log => {
            this.lastLog = log;
            this.status.isRunning = false;
            if (log.status === 'ok') {
                this.snackBar.open('Grading completed successfully!', 'Dismiss', { duration: 4000, panelClass: ['snack-success'] });
            } else {
                this.snackBar.open('Grading completed with errors.', 'Dismiss', { duration: 5000, panelClass: ['snack-error'] });
            }
        }));

        this.syncStatus();
        this.loadExams();
    }

    ngOnDestroy(): void {
        this.subs.unsubscribe();
        this.signalRService.stopConnection();
    }

    syncStatus(): void {
        this.testResultsService.getCurrentGradingRun().subscribe({
            next: (s) => this.status = s,
            error: () => console.warn('Could not sync runner status')
        });
    }

    loadExams(): void {
        this.examsService.getExams({ pageSize: 100 }).subscribe({
            next: (res) => this.exams = res.items || [],
        });
    }

    startGlobalRun(): void {
        this.isProcessing = true;
        this.errorMessage = null;
        this.testResultsService.startGradingRun().subscribe({
            next: (res) => {
                this.isProcessing = false;
                this.results = [];
                this.snackBar.open(res?.message || 'Runner started for all pending submissions!', 'Dismiss', { duration: 3000, panelClass: ['snack-success'] });
            },
            error: (err) => {
                this.isProcessing = false;
                if (err.status === 409) {
                    this.errorMessage = err?.error?.message || 'Runner is already active.';
                    this.syncStatus();
                } else {
                    this.errorMessage = 'Failed to start runner.';
                }
                this.snackBar.open(this.errorMessage!, 'Dismiss', { duration: 5000, panelClass: ['snack-error'] });
            }
        });
    }

    startExamRun(): void {
        if (!this.selectedExamId) return;

        this.isProcessing = true;
        this.errorMessage = null;
        this.testResultsService.startExamGradingRun({ examId: this.selectedExamId }).subscribe({
            next: (res) => {
                this.isProcessing = false;
                this.results = [];
                this.snackBar.open(res?.message || 'Runner started for selected exam!', 'Dismiss', { duration: 3000, panelClass: ['snack-success'] });
            },
            error: (err) => {
                this.isProcessing = false;
                if (err.status === 409) {
                    this.errorMessage = err?.error?.message || 'Runner is already active.';
                    this.syncStatus();
                } else {
                    this.errorMessage = 'Failed to start exam runner.';
                }
                this.snackBar.open(this.errorMessage!, 'Dismiss', { duration: 5000, panelClass: ['snack-error'] });
            }
        });
    }

    clearResults(): void {
        this.results = [];
        this.lastLog = null;
    }
}
