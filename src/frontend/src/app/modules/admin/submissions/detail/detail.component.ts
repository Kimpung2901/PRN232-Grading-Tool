import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { SubmissionsService } from '../../../../services/submissions.service';
import { ExamsService } from '../../../../services/exams.service';
import { SubmissionDto, ExamDto } from '../../../../api/models';

import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';

import { EditSubmissionDialogComponent } from './edit-submission-dialog/edit-submission-dialog.component';
import { ConfirmDialogComponent } from '../../../../shared/confirm-dialog/confirm-dialog.component';

@Component({
    selector: 'app-submissions-detail',
    standalone: true,
    imports: [
        CommonModule, RouterModule,
        MatDialogModule, MatSnackBarModule, MatButtonModule, MatIconModule,
        MatProgressSpinnerModule, MatTooltipModule, MatChipsModule, MatDividerModule
    ],
    templateUrl: './detail.component.html'
})
export class DetailComponent implements OnInit {
    submissionId = 0;
    submission: SubmissionDto | null = null;
    exam: ExamDto | null = null;
    isLoading = false;
    isProcessing = false;

    constructor(
        private route: ActivatedRoute,
        private router: Router,
        private submissionsService: SubmissionsService,
        private examsService: ExamsService,
        private _matDialog: MatDialog,
        private snackBar: MatSnackBar
    ) {
        const idParam = this.route.snapshot.paramMap.get('id');
        this.submissionId = idParam ? parseInt(idParam, 10) : 0;
    }

    ngOnInit(): void {
        if (this.submissionId) {
            this.loadData();
        }
    }

    loadData(): void {
        this.isLoading = true;
        this.submissionsService.getSubmissionById({ id: this.submissionId }).subscribe({
            next: (s) => {
                this.submission = s;
                if (s.examId) {
                    this.examsService.getExamById({ examId: s.examId }).subscribe({
                        next: (exam) => { this.exam = exam; this.isLoading = false; },
                        error: () => { this.isLoading = false; }
                    });
                } else {
                    this.isLoading = false;
                }
            },
            error: () => {
                this.isLoading = false;
                this.snackBar.open('Failed to load submission details.', 'Dismiss', { duration: 4000, panelClass: ['snack-error'] });
            }
        });
    }

    regrade(): void {
        if (!this.submissionId) return;
        this.isProcessing = true;
        this.submissionsService.regradeSubmission({ id: this.submissionId }).subscribe({
            next: (updated) => {
                this.isProcessing = false;
                this.submission = updated;
                this.snackBar.open('Submission requeued for grading.', 'Dismiss', { duration: 3000, panelClass: ['snack-success'] });
            },
            error: (err) => {
                this.isProcessing = false;
                const msg = err?.error?.detail || 'Regrade failed.';
                this.snackBar.open(msg, 'Dismiss', { duration: 4000, panelClass: ['snack-error'] });
            }
        });
    }

    requeue(): void {
        if (!this.submissionId) return;
        this.isProcessing = true;
        this.submissionsService.requeueSubmission({ id: this.submissionId }).subscribe({
            next: (updated) => {
                this.isProcessing = false;
                this.submission = updated;
                this.snackBar.open('Submission requeued.', 'Dismiss', { duration: 3000, panelClass: ['snack-success'] });
            },
            error: () => {
                this.isProcessing = false;
                this.snackBar.open('Requeue failed.', 'Dismiss', { duration: 4000, panelClass: ['snack-error'] });
            }
        });
    }

    editSubmission(): void {
        const dialogRef = this._matDialog.open(EditSubmissionDialogComponent, {
            data: { submission: this.submission }
        });

        dialogRef.afterClosed().subscribe((result) => {
            if (result) {
                this.submission = result;
                this.snackBar.open('Submission updated!', 'Dismiss', { duration: 3000, panelClass: ['snack-success'] });
            }
        });
    }

    deleteSubmission(): void {
        if (!this.submissionId) return;

        const dialogRef = this._matDialog.open(ConfirmDialogComponent, {
            data: {
                title: 'Delete Submission',
                message: `Are you sure you want to delete this submission from "${this.submission?.studentName}"? This action cannot be undone.`,
                confirmText: 'Delete',
                cancelText: 'Cancel',
                confirmColor: 'warn',
                icon: 'heroicons_outline:trash'
            }
        });

        dialogRef.afterClosed().subscribe(result => {
            if (!result) return;
            this.isProcessing = true;
            this.submissionsService.deleteSubmission({ id: this.submissionId }).subscribe({
                next: () => {
                    this.isProcessing = false;
                    this.snackBar.open('Submission deleted.', 'Dismiss', { duration: 3000 });
                    this.router.navigate(['/submissions'], { queryParamsHandling: 'preserve' });
                },
                error: (err) => {
                    this.isProcessing = false;
                    const msg = err?.error?.detail || 'Delete failed.';
                    this.snackBar.open(msg, 'Dismiss', { duration: 5000, panelClass: ['snack-error'] });
                }
            });
        });
    }

    getStatusLabel(): string {
        switch (this.submission?.status) {
            case 0: return 'Pending';
            case 1: return 'Queued';
            case 2: return 'Grading';
            case 3: return 'Graded';
            case 4: return 'Error';
            default: return 'Unknown';
        }
    }

    getStatusClass(): string {
        switch (this.submission?.status) {
            case 3: return 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-400';
            case 2: return 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400';
            case 4: return 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400';
            case 1: return 'bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400';
            default: return 'bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400';
        }
    }

    get fileName(): string {
        if (!this.submission?.filePath) return 'No file';
        const parts = this.submission.filePath.split(/[\\/]/);
        return parts[parts.length - 1];
    }
}
