import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { SubmissionsService } from '../../../../services/submissions.service';
import { SubmissionDto } from '../../../../api/models';

interface Submission {
    id: string;
    studentName: string;
    studentId: string;
    fileName: string;
    examId: string;
    examName: string;
    submittedAt: string;
    status: 'Pending' | 'Grading' | 'Graded' | 'Error';
    score?: number;
}

@Component({
    selector: 'app-submissions-dashboard',
    standalone: true,
    imports: [CommonModule, FormsModule, RouterModule],
    templateUrl: './dashboard.component.html'
})
export class DashboardComponent implements OnInit {
    searchQuery = '';
    selectedExam = '';
    
    exams = [
        { id: '1', name: 'Midterm PRN232 - Spring 2026' },
        { id: '2', name: 'Final PRN232 - Spring 2026' }
    ];

    submissions: SubmissionDto[] = [];

    constructor(private submissionsService: SubmissionsService) {}

    ngOnInit(): void {
        this.loadSubmissions();
    }

    loadSubmissions(): void {
        const examIdStr = this.selectedExam || '1'; // Defaulting to examId 1 for demo purposes
        const examIdNum = parseInt(examIdStr, 10);
        if(isNaN(examIdNum)) return;

        this.submissionsService.getSubmissions({ examId: examIdNum, search: this.searchQuery, page: 1, pageSize: 100 }).subscribe({
            next: (res) => {
                this.submissions = res.items || [];
            },
            error: (err) => console.error('Failed to load submissions', err)
        });
    }

    onFilterChange(): void {
        this.loadSubmissions();
    }

    get filteredSubmissions(): SubmissionDto[] {
        // Search is handled by API currently for the most part, but for local filtering:
        return this.submissions;
    }

    getStatusClass(status: number | undefined): string {
        switch(status) {
            case 3: return 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-400 border-emerald-200';
            case 2: return 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400 border-blue-200';
            case 1: return 'bg-orange-100 text-orange-700 dark:bg-orange-900/30 dark:text-orange-400 border-orange-200';
            case 4: return 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400 border-red-200';
            default: return 'bg-gray-100 text-gray-700';
        }
    }
    
    getStatusText(status: number | undefined): string {
        switch(status) {
            case 3: return 'Graded';
            case 2: return 'Grading';
            case 1: return 'Pending';
            case 4: return 'Error';
            default: return 'Unknown';
        }
    }
}
