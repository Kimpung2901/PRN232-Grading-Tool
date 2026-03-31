import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { ActivatedRoute, RouterModule } from '@angular/router';

interface GradingResult {
    id: string;
    testCaseName: string;
    status: 'Passed' | 'Failed' | 'Skipped';
    point: number;
    log: string;
}

@Component({
    selector: 'app-submissions-grading',
    standalone: true,
    imports: [CommonModule, RouterModule],
    templateUrl: './grading.component.html'
})
export class GradingComponent {
    submissionId = '';
    totalScore = 8.5;
    maxScore = 10.0;
    
    selectedLog: string | null = null;
    isLogOpen = false;
    
    results: GradingResult[] = [
        {
            id: 'r1',
            testCaseName: 'Login Success',
            status: 'Passed',
            point: 1.5,
            log: 'Request successful. Status code 200. Token received.'
        },
        {
            id: 'r2',
            testCaseName: 'Get User Profile',
            status: 'Passed',
            point: 1.0,
            log: 'Request successful. Status code 200. Profile match.'
        },
        {
            id: 'r3',
            testCaseName: 'Create Item',
            status: 'Failed',
            point: 0.0,
            log: 'AssertionError: expected status code 201 but got 400. \nResponse: {\n  "error": "Validation failed"\n}'
        },
        {
            id: 'r4',
            testCaseName: 'Get Item',
            status: 'Skipped',
            point: 0.0,
            log: 'Skipped due to dependency failure (Create Item).'
        }
    ];

    constructor(private route: ActivatedRoute) {
        this.submissionId = this.route.snapshot.paramMap.get('id') ?? 'SUB-1001';
    }

    getStatusClass(status: string): string {
        switch(status) {
            case 'Passed': return 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-400';
            case 'Failed': return 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400';
            case 'Skipped': return 'bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400';
            default: return 'bg-gray-100 text-gray-700 dark:bg-gray-800 dark:text-gray-400';
        }
    }

    viewLog(log: string): void {
        this.selectedLog = log;
        this.isLogOpen = true;
    }

    closeLog(): void {
        this.isLogOpen = false;
        setTimeout(() => {
            this.selectedLog = null;
        }, 300); // Wait for transition
    }
}
