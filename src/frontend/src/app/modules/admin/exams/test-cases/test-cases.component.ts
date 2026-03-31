import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { TestCasesService } from '../../../../services/test-cases.service';
import { TestCaseDto } from '../../../../api/models';

@Component({
    selector: 'app-exams-test-cases',
    standalone: true,
    imports: [CommonModule, RouterModule],
    templateUrl: './test-cases.component.html',
})
export class TestCasesComponent implements OnInit {
    examId = '';
    testCases: TestCaseDto[] = [];
    isLoading = false;
    isProcessing = false;

    constructor(
        private route: ActivatedRoute,
        private testCasesService: TestCasesService
    ) {
        this.examId = this.route.snapshot.paramMap.get('id') ?? '';
    }

    ngOnInit(): void {
        this.loadTestCases();
    }

    loadTestCases(): void {
        const examIdNum = parseInt(this.examId, 10);
        if (isNaN(examIdNum)) return;
        this.isLoading = true;
        this.testCasesService.getTestCases({ examId: examIdNum, page: 1, pageSize: 100 }).subscribe({
            next: (res) => { this.testCases = res.items || []; this.isLoading = false; },
            error: (err) => { this.isLoading = false; console.error('Failed to load test cases', err); }
        });
    }

    onFileUpload(event: Event): void {
        const input = event.target as HTMLInputElement;
        if (input.files && input.files.length > 0) {
            this.addNewTestCase(input.files[0]);
            input.value = '';
        }
    }

    addNewTestCase(file: File): void {
        const examIdNum = parseInt(this.examId, 10);
        if (isNaN(examIdNum)) return;
        this.isProcessing = true;
        this.testCasesService.createTestCase({
            examId: examIdNum,
            body: { File: file }
        }).subscribe({
            next: () => { this.isProcessing = false; this.loadTestCases(); },
            error: (err) => { this.isProcessing = false; console.error('Failed to add test case', err); }
        });
    }

    deleteTestCase(id: number | undefined): void {
        if (!id) return;
        const examIdNum = parseInt(this.examId, 10);
        if (isNaN(examIdNum)) return;
        if (!confirm('Delete this test case collection?')) return;
        this.isProcessing = true;
        this.testCasesService.deleteTestCase({ examId: examIdNum, id }).subscribe({
            next: () => { this.isProcessing = false; this.loadTestCases(); },
            error: (err) => { this.isProcessing = false; console.error('Failed to delete test case', err); }
        });
    }
}
