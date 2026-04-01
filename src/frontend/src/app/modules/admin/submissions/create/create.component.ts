import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { ExamsService } from '../../../../services/exams.service';
import { SubmissionsService } from '../../../../services/submissions.service';
import { ExamDto } from '../../../../api/models';

// Angular Material
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';

@Component({
    selector: 'app-submissions-create',
    standalone: true,
    imports: [
        CommonModule, FormsModule, ReactiveFormsModule, RouterModule,
        MatFormFieldModule, MatInputModule, MatSelectModule,
        MatButtonModule, MatIconModule, MatProgressSpinnerModule, MatSnackBarModule
    ],
    templateUrl: './create.component.html'
})
export class CreateComponent implements OnInit {
    form: FormGroup;
    selectedFile: File | null = null;
    isDragging = false;
    isSubmitting = false;
    isLoadingExams = false;
    exams: ExamDto[] = [];

    constructor(
        private fb: FormBuilder,
        private router: Router,
        private route: ActivatedRoute,
        private examsService: ExamsService,
        private submissionsService: SubmissionsService,
        private snackBar: MatSnackBar
    ) {
        this.form = this.fb.group({
            studentName: ['', [Validators.required, Validators.minLength(2)]],
            studentCode: ['', [Validators.required]],
            examId: ['', [Validators.required]]
        });
    }

    ngOnInit(): void {
        this.loadExams();
        this.route.queryParams.subscribe(params => {
            if (params['examId']) {
                this.form.patchValue({ examId: params['examId'] });
            }
        });
    }

    loadExams(): void {
        this.isLoadingExams = true;
        this.examsService.getExams({ pageSize: 100 }).subscribe({
            next: (res) => {
                this.exams = res.items || [];
                this.isLoadingExams = false;
            },
            error: () => {
                this.isLoadingExams = false;
                this.snackBar.open('Failed to load exams.', 'Dismiss', { duration: 4000, panelClass: ['snack-error'] });
            }
        });
    }

    onDragOver(event: DragEvent): void {
        event.preventDefault();
        this.isDragging = true;
    }

    onDragLeave(): void {
        this.isDragging = false;
    }

    onDrop(event: DragEvent): void {
        event.preventDefault();
        this.isDragging = false;
        const files = event.dataTransfer?.files;
        if (files && files.length > 0) {
            this.setFile(files[0]);
        }
    }

    onFileSelect(event: Event): void {
        const input = event.target as HTMLInputElement;
        if (input.files && input.files.length > 0) {
            this.setFile(input.files[0]);
        }
    }

    setFile(file: File): void {
        const validExtensions = ['.zip', '.rar', '.7z'];
        const fileName = file.name.toLowerCase();
        if (validExtensions.some(ext => fileName.endsWith(ext))) {
            this.selectedFile = file;
        } else {
            this.snackBar.open('Please select a valid archive file (.zip, .rar, .7z)', 'Dismiss', {
                duration: 3000, panelClass: ['snack-error']
            });
        }
    }

    removeFile(): void {
        this.selectedFile = null;
    }

    formatSize(bytes: number): string {
        if (bytes === 0) return '0 B';
        const k = 1024;
        const sizes = ['B', 'KB', 'MB', 'GB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return (bytes / Math.pow(k, i)).toFixed(1) + ' ' + sizes[i];
    }

    onSubmit(): void {
        if (this.form.invalid) {
            this.form.markAllAsTouched();
            return;
        }
        if (!this.selectedFile) {
            this.snackBar.open('Please upload a project archive file.', 'Dismiss', { duration: 3000, panelClass: ['snack-error'] });
            return;
        }
        this.isSubmitting = true;
        const val = this.form.value;
        this.submissionsService.createSubmission({
            examId: parseInt(val.examId, 10),
            body: {
                StudentName: val.studentName,
                StudentCode: val.studentCode,
                File: this.selectedFile
            }
        }).subscribe({
            next: (sub) => {
                this.isSubmitting = false;
                this.snackBar.open('Submission uploaded successfully!', 'View', { duration: 3000, panelClass: ['snack-success'] });
                this.router.navigate(['/submissions/data', sub.id]);
            },
            error: (err) => {
                this.isSubmitting = false;
                const msg = err?.error?.detail || 'Failed to upload submission. Please try again.';
                this.snackBar.open(msg, 'Dismiss', { duration: 5000, panelClass: ['snack-error'] });
            }
        });
    }

    onCancel(): void {
        this.router.navigate(['/submissions'], { queryParamsHandling: 'preserve' });
    }
}
