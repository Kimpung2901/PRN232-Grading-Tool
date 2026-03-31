import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, RouterModule, Router } from '@angular/router';
import { ExamsService } from '../../../../services/exams.service';
import { SubmissionsService } from '../../../../services/submissions.service';
import { TestResultsService } from '../../../../services/test-results.service';
import { ExamDto, SubmissionReportDto } from '../../../../api/models';

@Component({
    selector: 'app-exams-detail',
    standalone: true,
    imports: [CommonModule, RouterModule],
    templateUrl: './detail.component.html',
})
export class DetailComponent implements OnInit {
    id = '';
    detail: ExamDto | null = null;
    gradingResults: SubmissionReportDto[] = [];
    activeTab = 'overview'; // overview, test-cases, data
    isProcessing = false;

    constructor(
        private route: ActivatedRoute, 
        private examsService: ExamsService,
        private submissionsService: SubmissionsService,
        private testResultsService: TestResultsService,
        private router: Router
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
        
        this.examsService.getExamById({ examId: examIdNum }).subscribe({
            next: (res) => this.detail = res,
            error: (err) => console.error('Error fetching details', err)
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
        this.testResultsService.runExamGrading({ examId: examIdNum }).subscribe({
            next: () => {
                this.isProcessing = false;
                this.loadGradingResults();
            },
            error: (err) => {
                this.isProcessing = false;
                console.error('Error running grading', err);
            }
        });
    }

    requeueSubmissions(): void {
        const examIdNum = parseInt(this.id, 10);
        if (isNaN(examIdNum)) return;

        if (!confirm('Are you sure you want to requeue all submissions for this exam? This will reset their status to Pending.')) {
            return;
        }

        this.isProcessing = true;
        this.submissionsService.requeueExamSubmissions({ examId: examIdNum }).subscribe({
            next: () => {
                this.isProcessing = false;
                this.loadDetail();
                this.loadGradingResults();
            },
            error: (err) => {
                this.isProcessing = false;
                console.error('Error requeueing submissions', err);
            }
        });
    }

    deleteExam(): void {
        const examIdNum = parseInt(this.id, 10);
        if (isNaN(examIdNum)) return;

        if (!confirm('Are you sure you want to delete this exam? This action cannot be undone.')) {
            return;
        }

        this.isProcessing = true;
        this.examsService.deleteExam({ examId: examIdNum }).subscribe({
            next: () => {
                this.isProcessing = false;
                this.router.navigate(['/exams', 'dashboard']);
            },
            error: (err) => {
                this.isProcessing = false;
                console.error('Error deleting exam', err);
            }
        });
    }

    // Placeholder for edit (could open a dialog or toggle edit mode)
    editExam(): void {
        const newName = prompt('Enter new exam name:', this.detail?.examName);
        if (newName && newName !== this.detail?.examName) {
            const examIdNum = parseInt(this.id, 10);
            this.examsService.updateExam({ 
                examId: examIdNum, 
                body: { ExamName: newName } 
            }).subscribe({
                next: (res) => this.detail = res,
                error: (err) => console.error('Error updating exam', err)
            });
        }
    }
}
