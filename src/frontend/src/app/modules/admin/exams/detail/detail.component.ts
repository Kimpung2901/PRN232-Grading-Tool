import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { ActivatedRoute, RouterModule } from '@angular/router';

interface ExamDetail {
    id: string;
    name: string;
    semester: string;
    status: 'Active' | 'Draft' | 'Completed';
    description: string;
    collectionFilePath: string;
    databaseFilePath: string;
    testCaseCount: number;
    submissionCount: number;
    createdAt: string;
}

@Component({
    selector: 'app-exams-detail',
    standalone: true,
    imports: [CommonModule, RouterModule],
    templateUrl: './detail.component.html',
})
export class DetailComponent {
    id = '';
    detail: ExamDetail | null = null;
    activeTab = 'overview'; // overview, test-cases, data

    constructor(private route: ActivatedRoute) {
        const idParam = this.route.snapshot.paramMap.get('id') ?? 'EXM-101';
        this.id = idParam;
        
        // Mock data
        this.detail = {
            id: this.id,
            name: 'Midterm PRN232',
            semester: 'Spring 2026',
            status: 'Active',
            description: 'Midterm examination for PRN232 course. Covers all chapters up to 5.',
            collectionFilePath: '/uploads/collections/midterm_spring_2026.json',
            databaseFilePath: '/uploads/database/db_midterm.sql',
            testCaseCount: 15,
            submissionCount: 120,
            createdAt: '2026-03-01T08:00:00Z'
        };
    }
}
