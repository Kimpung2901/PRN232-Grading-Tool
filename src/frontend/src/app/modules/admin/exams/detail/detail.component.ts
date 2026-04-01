import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, RouterModule, Router } from '@angular/router';
import { ExamsService } from '../../../../services/exams.service';
import { SubmissionsService } from '../../../../services/submissions.service';
import { TestResultsService } from '../../../../services/test-results.service';
import { ExamDto, SubmissionReportDto } from '../../../../api/models';

import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { EditExamDialogComponent } from './edit-exam-dialog/edit-exam-dialog.component';

@Component({
    selector: 'app-exams-detail',
    standalone: true,
    imports: [CommonModule, RouterModule, MatDialogModule],
    templateUrl: './detail.component.html',
})
export class DetailComponent implements OnInit {
    id = '';
    detail: ExamDto | null = null;
    gradingResults: SubmissionReportDto[] = [];
    isLoading = false;
    isProcessing = false;

    constructor(
        private route: ActivatedRoute,
        private examsService: ExamsService,
        private submissionsService: SubmissionsService,
        private testResultsService: TestResultsService,
        private router: Router,
        private _matDialog: MatDialog
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
            error: (err) => { this.isLoading = false; console.error('Error fetching details', err); }
        });
    }

    loadGradingResults(): void {
        const examIdNum = parseInt(this.id, 10);
        if (isNaN(examIdNum)) return;
        this.submissionsService.getGradingResults({ examId: examIdNum }).subscribe({
            next: (res) => this.gradingResults = res,
            error: (err) => console.error('Error fetching grading results', err)
        });
    }

    runGrading(): void {
        const examIdNum = parseInt(this.id, 10);
        if (isNaN(examIdNum)) return;
        this.isProcessing = true;
        this.testResultsService.startExamGradingRun({ examId: examIdNum }).subscribe({
            next: () => { this.isProcessing = false; this.loadGradingResults(); },
            error: (err) => { this.isProcessing = false; console.error('Error running grading', err); }
        });
    }

    requeueSubmissions(): void {
        const examIdNum = parseInt(this.id, 10);
        if (isNaN(examIdNum)) return;
        if (!confirm('Requeue all submissions for this exam? Their status will reset to Pending.')) return;
        this.isProcessing = true;
        this.submissionsService.requeueExamSubmissions({ examId: examIdNum }).subscribe({
            next: () => { this.isProcessing = false; this.loadGradingResults(); },
            error: (err) => { this.isProcessing = false; console.error('Error requeueing', err); }
        });
    }

    deleteExam(): void {
        const examIdNum = parseInt(this.id, 10);
        if (isNaN(examIdNum)) return;
        if (!confirm('Delete this exam? This action cannot be undone.')) return;
        this.isProcessing = true;
        this.examsService.deleteExam({ examId: examIdNum }).subscribe({
            next: () => { this.isProcessing = false; this.router.navigate(['/exams']); },
            error: (err) => { this.isProcessing = false; console.error('Error deleting exam', err); }
        });
    }

    editExam(): void {
        const dialogRef = this._matDialog.open(EditExamDialogComponent, {
            data: { exam: this.detail }
        });

        dialogRef.afterClosed().subscribe((result) => {
            if (result) {
                this.detail = result;
            }
        });
    }
}
