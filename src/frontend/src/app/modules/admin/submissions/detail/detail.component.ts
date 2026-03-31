import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { SubmissionsService } from '../../../../services/submissions.service';
import { ExamsService } from '../../../../services/exams.service';
import { SubmissionDto, ExamDto } from '../../../../api/models';
import { forkJoin } from 'rxjs';

@Component({
    selector: 'app-submissions-detail',
    standalone: true,
    imports: [CommonModule, RouterModule],
    templateUrl: './detail.component.html'
})
export class DetailComponent {
    submissionId: number;
    activeTab = 'overview'; 
    submission: SubmissionDto | null = null;
    exam: ExamDto | null = null;
    isProcessing = false;
    
    constructor(
        private route: ActivatedRoute,
        private submissionsService: SubmissionsService,
        private examsService: ExamsService
    ) {
        const idParam = this.route.snapshot.paramMap.get('id');
        this.submissionId = idParam ? parseInt(idParam, 10) : 0;
        
        if (this.submissionId) {
            this.loadData();
        }
    }

    loadData(): void {
        this.isProcessing = true;
        this.submissionsService.getSubmissionById({ id: this.submissionId }).subscribe({
            next: (submission) => {
                this.submission = submission;
                this.isProcessing = false;
                if (submission.examId) {
                    this.examsService.getExamById({ examId: submission.examId }).subscribe({
                        next: (exam) => this.exam = exam,
                        error: (err) => console.error('Failed to load exam', err)
                    });
                }
            },
            error: (err) => {
                this.isProcessing = false;
                console.error('Failed to load submission', err);
            }
        });
    }

    regrade(): void {
        if (!this.submissionId) return;
        this.isProcessing = true;
        this.submissionsService.regradeRequest({ id: this.submissionId }).subscribe({
            next: () => {
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
                this.loadData();
            },
            error: (err) => {
                this.isProcessing = false;
                console.error('Requeue failed', err);
            }
        });
    }

    get studentName(): string {
        return this.submission?.studentName || 'Unknown Student';
    }

    get studentCode(): string {
        return this.submission?.studentCode || 'N/A';
    }

    get examName(): string {
        return this.exam?.examName || 'Loading...';
    }

    get statusText(): string {
        const s = this.submission?.status;
        switch(s) {
            case 1: return 'Pending';
            case 2: return 'Grading';
            case 3: return 'Graded';
            case 4: return 'Error';
            default: return 'Unknown';
        }
    }

    get fileName(): string {
        if (!this.submission?.filePath) return 'No file';
        const parts = this.submission.filePath.split(/[\\/]/);
        return parts[parts.length - 1];
    }

    downloadSource(): void {
        console.log('Downloading file:', this.submission?.filePath);
    }
}
