import {
    animate,
    state,
    style,
    transition,
    trigger,
} from '@angular/animations';
import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { MatSelectModule } from '@angular/material/select';
import { MatOptionModule } from '@angular/material/core';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

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
export class DashboardComponent {
    // Filter state
    filterOpen = false;
    searchQuery = '';
    selectedSemester = 'Spring 2026';

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

    exams = [
        {
            id: 'EXM-101',
            name: 'Midterm PRN232',
            semester: 'Spring 2026',
            testCases: 15,
            submissions: 120,
            status: 'Active',
            statusColor: 'bg-emerald-100 text-emerald-700'
        },
        {
            id: 'EXM-102',
            name: 'Final PRN232',
            semester: 'Spring 2026',
            testCases: 25,
            submissions: 0,
            status: 'Draft',
            statusColor: 'bg-gray-100 text-gray-700'
        },
        {
            id: 'EXM-103',
            name: 'Assignment 1',
            semester: 'Fall 2025',
            testCases: 10,
            submissions: 145,
            status: 'Completed',
            statusColor: 'bg-blue-100 text-blue-700'
        },
        {
            id: 'EXM-104',
            name: 'Assignment 2',
            semester: 'Fall 2025',
            testCases: 12,
            submissions: 142,
            status: 'Completed',
            statusColor: 'bg-blue-100 text-blue-700'
        }
    ];

    constructor(private router: Router) {}

    toggleFilter(): void {
        this.filterOpen = !this.filterOpen;
    }

    applyFilter(): void {
        console.log('Filter applied. Semester:', this.selectedSemester);
        // Integrate API
    }

    resetFilter(): void {
        this.selectedSemester = 'Spring 2026';
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
