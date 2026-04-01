import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { SignalRService, RunnerStatus, RunnerCompletionLog } from '../../../../services/signalr.service';
import { TestResultsService } from '../../../../services/test-results.service';
import { ExamsService } from '../../../../services/exams.service';
import { ExamDto, TestResultRequest } from '../../../../api/models';
import { Subscription } from 'rxjs';

@Component({
    selector: 'app-grading-runner',
    standalone: true,
    imports: [CommonModule, FormsModule, RouterModule],
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

    private subs = new Subscription();

    constructor(
        private signalRService: SignalRService,
        private testResultsService: TestResultsService,
        private examsService: ExamsService
    ) {}

    ngOnInit(): void {
        // Start SignalR connection
        this.signalRService.startConnection();

        // Subscribe to SignalR events
        this.subs.add(this.signalRService.status$.subscribe(s => {
            this.status = s;
            if (s.isRunning) {
                this.errorMessage = null; 
            }
        }));

        this.subs.add(this.signalRService.testResult$.subscribe(r => {
            this.results.unshift(r);
            // Limit results to last 50 for performance
            if (this.results.length > 50) this.results.pop();
        }));

        this.subs.add(this.signalRService.completion$.subscribe(log => {
            this.lastLog = log;
            this.status.isRunning = false;
        }));

        // Initial sync
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
            error: (err) => console.error('Failed to sync runner status', err)
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
            next: () => {
                this.isProcessing = false;
                this.results = []; // Clear previous results
            },
            error: (err) => {
                this.isProcessing = false;
                if (err.status === 409) {
                    this.errorMessage = 'Runner is already active.';
                    this.syncStatus();
                } else {
                    this.errorMessage = 'Failed to start runner.';
                }
            }
        });
    }

    startExamRun(): void {
        if (!this.selectedExamId) return;
        
        this.isProcessing = true;
        this.errorMessage = null;
        this.testResultsService.startExamGradingRun({ examId: this.selectedExamId }).subscribe({
            next: () => {
                this.isProcessing = false;
                this.results = [];
            },
            error: (err) => {
                this.isProcessing = false;
                if (err.status === 409) {
                    this.errorMessage = 'Runner is already active.';
                    this.syncStatus();
                } else {
                    this.errorMessage = 'Failed to start exam runner.';
                }
            }
        });
    }

    clearResults(): void {
        this.results = [];
        this.lastLog = null;
    }
}
