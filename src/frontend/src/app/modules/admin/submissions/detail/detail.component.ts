import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { ActivatedRoute, RouterModule } from '@angular/router';

interface SubmissionDetail {
    id: string;
    studentName: string;
    studentId: string;
    examName: string;
    fileName: string;
    fileSize: string;
    submittedAt: string;
    source: 'Manual' | 'Student Portal';
    status: 'Pending' | 'Grading' | 'Graded' | 'Error';
    score: number | null;
    lastError: string | null;
}

@Component({
    selector: 'app-submissions-detail',
    standalone: true,
    imports: [CommonModule, RouterModule],
    templateUrl: './detail.component.html'
})
export class DetailComponent {
    id = '';
    activeTab = 'overview'; // overview, data, grading
    detail: SubmissionDetail | null = null;
    
    constructor(private route: ActivatedRoute) {
        this.id = this.route.snapshot.paramMap.get('id') ?? 'SUB-1001';
        
        // Mock data
        this.detail = {
            id: this.id,
            studentName: 'Nguyen Van A',
            studentId: 'SE15001',
            examName: 'Midterm PRN232 - Spring 2026',
            fileName: 'SE15001_Midterm.zip',
            fileSize: '4.2 MB',
            submittedAt: 'Mar 17, 2026 09:15',
            source: 'Student Portal',
            status: 'Error',
            score: null,
            lastError: 'Build failed: Cannot find module "express". Ensure all dependencies are included in package.json and node_modules is not required.'
        };
    }

    downloadSource(): void {
        console.log('Downloading file:', this.detail?.fileName);
    }
}
