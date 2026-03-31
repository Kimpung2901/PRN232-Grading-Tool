import {
    animate,
    state,
    style,
    transition,
    trigger,
} from '@angular/animations';
import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { MatSelectModule } from '@angular/material/select';
import { MatOptionModule } from '@angular/material/core';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { ExamsService } from '../../../../services/exams.service';
import { TestResultsService } from '../../../../services/test-results.service';
import { ExamDto } from '../../../../api/models';

@Component({
    selector: 'app-exams-dashboard',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        RouterModule,
        MatSelectModule,
        MatOptionModule,
        MatFormFieldModule,
        MatInputModule
    ],
    templateUrl: './dashboard.component.html',
    animations: [
        trigger('collapseFilter', [
            state(
                'open',
                style({
                    height: '*',
                    opacity: 1,
                    overflow: 'hidden',
                    marginTop: '20px',
                })
            ),
            state(
                'closed',
                style({
                    height: '0px',
                    opacity: 0,
                    overflow: 'hidden',
                    marginTop: '0px',
                })
            ),
            transition('open <=> closed', [animate('300ms ease-in-out')]),
        ]),
    ],
})
export class DashboardComponent implements OnInit {
    // Filter state
    filterOpen = false;
    searchQuery = '';
    selectedSemester = 'Spring 2026';
    isProcessing = false;

    semesters = [
        'Spring 2026',
        'Fall 2025',
        'Summer 2025',
        'Spring 2025',
        'All Semesters'
    ];

    stats = [
        {
            title: 'Total Exams',
            value: '42',
            trend: '+5%',
            up: true,
            bg: 'bg-indigo-100',
            icon: 'text-indigo-500',
        },
        {
            title: 'Active Exams',
            value: '12',
            trend: '+12%',
            up: true,
            bg: 'bg-emerald-100',
            icon: 'text-emerald-500',
        },
        {
            title: 'Total Submissions',
            value: '1,284',
            trend: '+24%',
            up: true,
            bg: 'bg-blue-100',
            icon: 'text-blue-500',
        },
        {
            title: 'Needs Grading',
            value: '84',
            trend: '-2%',
            up: false,
            bg: 'bg-orange-100',
            icon: 'text-orange-500',
        }
    ];

    exams: ExamDto[] = [];

    constructor(
        private router: Router,
        private examsService: ExamsService,
        private testResultsService: TestResultsService
    ) {}

    ngOnInit(): void {
        this.loadExams();
    }

    loadExams(): void {
        this.examsService.getExams({ search: this.searchQuery || undefined, page: 1, pageSize: 100 }).subscribe({
            next: (res) => {
                this.exams = res.items || [];
                this.updateStats(res.total || 0);
            },
            error: (err) => console.error('Failed to load exams', err)
        });
    }

    updateStats(totalCount: number): void {
        this.stats[0].value = totalCount.toString();
        // Other stats might remain mock or be calculated if data is available
    }

    runAllGrading(): void {
        if (!confirm('Are you sure you want to run the grading pipeline for all exams?')) {
            return;
        }

        this.isProcessing = true;
        this.testResultsService.runGrading().subscribe({
            next: () => {
                this.isProcessing = false;
                alert('Grading pipeline started for all exams.');
            },
            error: (err) => {
                this.isProcessing = false;
                console.error('Failed to start global grading', err);
            }
        });
    }

    toggleFilter(): void {
        this.filterOpen = !this.filterOpen;
    }

    applyFilter(): void {
        this.loadExams();
    }

    resetFilter(): void {
        this.selectedSemester = 'Spring 2026';
        this.searchQuery = '';
        this.loadExams();
    }

    createNewExam(): void {
        this.router.navigate(['/exams/create']);
    }

    viewDetails(id: string): void {
        this.router.navigate(['/exams/detail', id]);
    }

    manageTestCases(id: string): void {
        this.router.navigate(['/exams/detail', id, 'test-cases']);
    }
}
