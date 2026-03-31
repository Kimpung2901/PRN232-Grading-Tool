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

        this.testCasesService.getTestCases({ examId: examIdNum, page: 1, pageSize: 100 }).subscribe({
            next: (res) => this.testCases = res.items || [],
            error: (err) => console.error('Failed to load test cases', err)
        });
    }

    addNewTestCase(): void {
        console.log('Add new test case');
    }
}
