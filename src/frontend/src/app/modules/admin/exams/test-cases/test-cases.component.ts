import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { TestCasesService } from '../../../../services/test-cases.service';
import { TestCaseDto } from '../../../../api/models';

// Angular Material
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatChipsModule } from '@angular/material/chips';
import { ConfirmDialogComponent } from '../../../../shared/confirm-dialog/confirm-dialog.component';

@Component({
    selector: 'app-exams-test-cases',
    standalone: true,
    imports: [
        CommonModule, RouterModule,
        MatTableModule, MatButtonModule, MatIconModule,
        MatProgressSpinnerModule, MatTooltipModule, MatSnackBarModule,
        MatDialogModule, MatChipsModule
    ],
    templateUrl: './test-cases.component.html',
})
export class TestCasesComponent implements OnInit {
    examId = '';
    testCases: TestCaseDto[] = [];
    isLoading = false;
    isProcessing = false;
    displayedColumns: string[] = ['id', 'filePath', 'actions'];

    constructor(
        private route: ActivatedRoute,
        private testCasesService: TestCasesService,
        private snackBar: MatSnackBar,
        private dialog: MatDialog
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
            error: () => {
                this.isLoading = false;
                this.snackBar.open('Failed to load test cases.', 'Dismiss', { duration: 4000, panelClass: ['snack-error'] });
            }
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
            next: () => {
                this.isProcessing = false;
                this.snackBar.open('Test case uploaded successfully!', 'Dismiss', { duration: 3000, panelClass: ['snack-success'] });
                this.loadTestCases();
            },
            error: (err) => {
                this.isProcessing = false;
                const msg = err?.error?.detail || 'Failed to upload test case.';
                this.snackBar.open(msg, 'Dismiss', { duration: 5000, panelClass: ['snack-error'] });
            }
        });
    }

    deleteTestCase(id: number | undefined, filePath: string | undefined): void {
        if (!id) return;
        const examIdNum = parseInt(this.examId, 10);
        if (isNaN(examIdNum)) return;

        const dialogRef = this.dialog.open(ConfirmDialogComponent, {
            data: {
                title: 'Delete Test Case',
                message: `Are you sure you want to delete test case #${id}? This action cannot be undone.`,
                confirmText: 'Delete',
                cancelText: 'Cancel',
                confirmColor: 'warn',
                icon: 'heroicons_outline:trash'
            }
        });

        dialogRef.afterClosed().subscribe(result => {
            if (!result) return;
            this.isProcessing = true;
            this.testCasesService.deleteTestCase({ examId: examIdNum, id }).subscribe({
                next: () => {
                    this.isProcessing = false;
                    this.snackBar.open('Test case deleted.', 'Dismiss', { duration: 3000, panelClass: ['snack-success'] });
                    this.loadTestCases();
                },
                error: () => {
                    this.isProcessing = false;
                    this.snackBar.open('Failed to delete test case.', 'Dismiss', { duration: 4000, panelClass: ['snack-error'] });
                }
            });
        });
    }

    getFileBasename(path: string | undefined): string {
        if (!path) return 'No path';
        const parts = path.split(/[\\/]/);
        return parts[parts.length - 1];
    }
}
