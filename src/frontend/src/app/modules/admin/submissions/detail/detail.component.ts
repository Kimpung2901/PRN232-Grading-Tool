import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { SubmissionsService } from '../../../../services/submissions.service';
import { ExamsService } from '../../../../services/exams.service';
import { SubmissionDto, ExamDto } from '../../../../api/models';

@Component({
    selector: 'app-submissions-detail',
    standalone: true,
    imports: [CommonModule, RouterModule],
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
        private examsService: ExamsService
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
                        next: (exam) => {
                            this.exam = exam;
                            this.isLoading = false;
                        },
                        error: (err) => {
                            this.isLoading = false;
                            console.error('Failed to load exam', err);
                        }
                    });
                } else {
                    this.isLoading = false;
                }
            },
            error: (err) => {
                this.isLoading = false;
                console.error('Failed to load submission', err);
            }
        });
    }

    regrade(): void {
        if (!this.submissionId) return;
        this.isProcessing = true;
        this.submissionsService.regradeSubmission({ id: this.submissionId }).subscribe({
            next: () => {
                this.isProcessing = false;
                this.loadData();
            },
            error: (err) => {
                this.isProcessing = false;
                console.error('Regrade failed', err);
            }
        });
    }

    requeue(): void {
        if (!this.submissionId) return;
        this.isProcessing = true;
        this.submissionsService.requeueSubmission({ id: this.submissionId }).subscribe({
            next: () => {
                this.isProcessing = false;
                this.loadData();
            },
            error: (err) => {
                this.isProcessing = false;
                console.error('Requeue failed', err);
            }
        });
    }

    deleteSubmission(): void {
        if (!this.submissionId) return;
        if (!confirm('Are you sure you want to delete this submission? This action cannot be undone.')) return;
        
        this.isProcessing = true;
        this.submissionsService.deleteSubmission({ id: this.submissionId }).subscribe({
            next: () => {
                this.isProcessing = false;
                this.router.navigate(['/submissions'], { queryParamsHandling: 'preserve' });
            },
            error: (err) => {
                this.isProcessing = false;
                console.error('Delete failed', err);
            }
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
            default: return 'bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400';
        }
    }

    get fileName(): string {
        if (!this.submission?.filePath) return 'No file';
        const parts = this.submission.filePath.split(/[\\/]/);
        return parts[parts.length - 1];
    }
}
