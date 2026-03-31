import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';

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
export class DashboardComponent {
    searchQuery = '';
    selectedExam = '';
    
    exams = [
        { id: 'EXM-101', name: 'Midterm PRN232 - Spring 2026' },
        { id: 'EXM-102', name: 'Final PRN232 - Spring 2026' }
    ];

    submissions: Submission[] = [
        {
            id: 'SUB-1001',
            studentName: 'Nguyen Van A',
            studentId: 'SE15001',
            fileName: 'SE15001_Midterm.zip',
            examId: 'EXM-101',
            examName: 'Midterm PRN232',
            submittedAt: 'Mar 17, 2026 09:15',
            status: 'Graded',
            score: 8.5
        },
        {
            id: 'SUB-1002',
            studentName: 'Tran Thi B',
            studentId: 'SE15002',
            fileName: 'SE15002_Midterm.zip',
            examId: 'EXM-101',
            examName: 'Midterm PRN232',
            submittedAt: 'Mar 17, 2026 09:18',
            status: 'Error'
        },
        {
            id: 'SUB-1003',
            studentName: 'Le Van C',
            studentId: 'SE15003',
            fileName: 'SE15003_Midterm.zip',
            examId: 'EXM-101',
            examName: 'Midterm PRN232',
            submittedAt: 'Mar 17, 2026 09:20',
            status: 'Grading'
        },
        {
            id: 'SUB-1004',
            studentName: 'Pham Quoc D',
            studentId: 'SE15004',
            fileName: 'SE15004_Midterm.zip',
            examId: 'EXM-101',
            examName: 'Midterm PRN232',
            submittedAt: 'Mar 17, 2026 09:25',
            status: 'Pending'
        }
    ];

    get filteredSubmissions(): Submission[] {
        return this.submissions.filter(sub => {
            const matchExam = this.selectedExam ? sub.examId === this.selectedExam : true;
            const matchSearch = !this.searchQuery.trim() || 
                sub.studentName.toLowerCase().includes(this.searchQuery.toLowerCase()) ||
                sub.studentId.toLowerCase().includes(this.searchQuery.toLowerCase());
            return matchExam && matchSearch;
        });
    }

    getStatusClass(status: string): string {
        switch(status) {
            case 'Graded': return 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-400 border-emerald-200';
            case 'Grading': return 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400 border-blue-200';
            case 'Pending': return 'bg-orange-100 text-orange-700 dark:bg-orange-900/30 dark:text-orange-400 border-orange-200';
            case 'Error': return 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400 border-red-200';
            default: return 'bg-gray-100 text-gray-700';
        }
    }
}
