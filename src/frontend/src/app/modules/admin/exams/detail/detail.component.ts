import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, RouterModule, Router } from '@angular/router';
import { ExamsService } from '../../../../services/exams.service';
import { SubmissionsService } from '../../../../services/submissions.service';
import { TestResultsService } from '../../../../services/test-results.service';
import { ExamDto, SubmissionReportDto } from '../../../../api/models';

import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';
import { MatBadgeModule } from '@angular/material/badge';

import { EditExamDialogComponent } from './edit-exam-dialog/edit-exam-dialog.component';
import { ConfirmDialogComponent } from '../../../../shared/confirm-dialog/confirm-dialog.component';

@Component({
    selector: 'app-exams-detail',
    standalone: true,
    imports: [
        CommonModule, RouterModule,
        MatDialogModule, MatSnackBarModule, MatButtonModule, MatIconModule,
        MatTableModule, MatProgressSpinnerModule, MatTooltipModule,
        MatChipsModule, MatDividerModule, MatBadgeModule
    ],
    templateUrl: './detail.component.html',
})
export class DetailComponent implements OnInit {
    id = '';
    detail: ExamDto | null = null;
    gradingResults: SubmissionReportDto[] = [];
    isLoading = false;
    isProcessing = false;

    displayedColumns: string[] = ['student', 'code', 'score', 'status', 'lastError'];

    constructor(
        private route: ActivatedRoute,
        private examsService: ExamsService,
        private submissionsService: SubmissionsService,
        private testResultsService: TestResultsService,
        private router: Router,
        private _matDialog: MatDialog,
        private snackBar: MatSnackBar
    ) {
        this.id = this.route.snapshot.paramMap.get('id') ?? '';
    }

    ngOnInit(): void {
        this.loadDetail();
        this.loadGradingResults();
    }

    loadDetail(): void {
        const examIdNum = parseInt(this.id, 10);
        if (isNaN(examIdNum)) return;
        this.isLoading = true;
        this.examsService.getExamById({ examId: examIdNum }).subscribe({
            next: (res) => { this.detail = res; this.isLoading = false; },
            error: () => {
                this.isLoading = false;
                this.snackBar.open('Failed to load exam details.', 'Dismiss', { duration: 4000, panelClass: ['snack-error'] });
            }
        });
    }

    loadGradingResults(): void {
        const examIdNum = parseInt(this.id, 10);
        if (isNaN(examIdNum)) return;
        this.submissionsService.getGradingResults({ examId: examIdNum }).subscribe({
            next: (res) => this.gradingResults = res,
            error: () => console.warn('Could not load grading results')
        });
    }

    runGrading(): void {
        const examIdNum = parseInt(this.id, 10);
        if (isNaN(examIdNum)) return;
        this.isProcessing = true;
        this.testResultsService.startExamGradingRun({ examId: examIdNum }).subscribe({
            next: (res) => {
                this.isProcessing = false;
                this.snackBar.open(res?.message || 'Grading started successfully!', 'Dismiss', { duration: 3000, panelClass: ['snack-success'] });
                this.loadGradingResults();
            },
            error: (err) => {
                this.isProcessing = false;
                const msg = err?.error?.message || err?.error?.detail || 'Failed to start grading run.';
                this.snackBar.open(msg, 'Dismiss', { duration: 5000, panelClass: ['snack-error'] });
            }
        });
    }

    requeueSubmissions(): void {
        const examIdNum = parseInt(this.id, 10);
        if (isNaN(examIdNum)) return;

        const dialogRef = this._matDialog.open(ConfirmDialogComponent, {
            data: {
                title: 'Requeue All Submissions',
                message: 'This will reset all submission scores/reports and requeue them for grading. This action cannot be undone.',
                confirmText: 'Requeue All',
                cancelText: 'Cancel',
                confirmColor: 'warn',
                icon: 'heroicons_outline:arrow-path'
            }
        });

        dialogRef.afterClosed().subscribe(result => {
            if (!result) return;
            this.isProcessing = true;
            this.submissionsService.requeueExamSubmissions({ examId: examIdNum }).subscribe({
                next: () => {
                    this.isProcessing = false;
                    this.snackBar.open('All submissions requeued for regrading.', 'Dismiss', { duration: 3000, panelClass: ['snack-success'] });
                    this.loadGradingResults();
                },
                error: () => {
                    this.isProcessing = false;
                    this.snackBar.open('Failed to requeue submissions.', 'Dismiss', { duration: 4000, panelClass: ['snack-error'] });
                }
            });
        });
    }

    deleteExam(): void {
        const examIdNum = parseInt(this.id, 10);
        if (isNaN(examIdNum)) return;

        const dialogRef = this._matDialog.open(ConfirmDialogComponent, {
            data: {
                title: 'Delete Exam',
                message: `Are you sure you want to delete "${this.detail?.examName}"? This action cannot be undone.`,
                confirmText: 'Delete',
                cancelText: 'Cancel',
                confirmColor: 'warn',
                icon: 'heroicons_outline:trash'
            }
        });

        dialogRef.afterClosed().subscribe(result => {
            if (!result) return;
            this.isProcessing = true;
            this.examsService.deleteExam({ examId: examIdNum }).subscribe({
                next: () => {
                    this.isProcessing = false;
                    this.snackBar.open('Exam deleted successfully.', 'Dismiss', { duration: 3000 });
                    this.router.navigate(['/exams']);
                },
                error: (err) => {
                    this.isProcessing = false;
                    const msg = err?.error?.detail || 'Failed to delete exam.';
                    this.snackBar.open(msg, 'Dismiss', { duration: 5000, panelClass: ['snack-error'] });
                }
            });
        });
    }

    editExam(): void {
        const dialogRef = this._matDialog.open(EditExamDialogComponent, {
            data: { exam: this.detail }
        });

        dialogRef.afterClosed().subscribe((result) => {
            if (result) {
                this.detail = result;
                this.snackBar.open('Exam updated successfully!', 'Dismiss', { duration: 3000, panelClass: ['snack-success'] });
            }
        });
    }

    getStatusChipColor(status: string | undefined): string {
        switch (status?.toLowerCase()) {
            case 'graded': return 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-400';
            case 'error': return 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400';
            case 'grading': return 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400';
            default: return 'bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400';
        }
    }
}
