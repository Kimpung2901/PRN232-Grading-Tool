import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { ActivatedRoute, RouterModule } from '@angular/router';

interface TestCase {
    id: string;
    name: string;
    point: number;
    postmanItemId: string;
    dependencyTestCaseId: string | null;
    isGroup?: boolean; // For tree-view styling
    expanded?: boolean;
}

@Component({
    selector: 'app-exams-test-cases',
    standalone: true,
    imports: [CommonModule, RouterModule],
    templateUrl: './test-cases.component.html',
})
export class TestCasesComponent {
    examId = '';
    
    // Flat list structured to simulate a tree
    testCases: TestCase[] = [
        {
            id: 'tc1',
            name: 'Authentication Group',
            point: 0,
            postmanItemId: '',
            dependencyTestCaseId: null,
            isGroup: true,
            expanded: true
        },
        {
            id: 'tc1a',
            name: 'Login Success',
            point: 1.5,
            postmanItemId: 'postman-req-123',
            dependencyTestCaseId: null,
            isGroup: false
        },
        {
            id: 'tc1b',
            name: 'Get User Profile',
            point: 1.0,
            postmanItemId: 'postman-req-124',
            dependencyTestCaseId: 'tc1a', // depends on Login Success
            isGroup: false
        },
        {
            id: 'tc2',
            name: 'API Endpoints Group',
            point: 0,
            postmanItemId: '',
            dependencyTestCaseId: null,
            isGroup: true,
            expanded: true
        },
        {
            id: 'tc2a',
            name: 'Create Item',
            point: 2.0,
            postmanItemId: 'postman-req-201',
            dependencyTestCaseId: 'tc1a', // depends on Login
            isGroup: false
        },
        {
            id: 'tc2b',
            name: 'Get Item',
            point: 1.0,
            postmanItemId: 'postman-req-202',
            dependencyTestCaseId: 'tc2a', // depends on Create Item
            isGroup: false
        }
    ];

    constructor(private route: ActivatedRoute) {
        this.examId = this.route.snapshot.paramMap.get('id') ?? 'EXM-101';
    }

    getDependencyName(depId: string | null): string {
        if (!depId) return '-';
        const tc = this.testCases.find(t => t.id === depId);
        return tc ? tc.name : depId;
    }

    toggleGroup(tc: TestCase): void {
        if (tc.isGroup) tc.expanded = !tc.expanded;
    }

    addNewTestCase(): void {
        console.log('Add new test case');
    }
}
