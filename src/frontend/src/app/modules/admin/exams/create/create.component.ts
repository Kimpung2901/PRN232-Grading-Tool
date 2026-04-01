import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { ExamsService } from '../../../../services/exams.service';

// Angular Material
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';

@Component({
    selector: 'app-exams-create',
    standalone: true,
    imports: [
        CommonModule, FormsModule, ReactiveFormsModule, RouterModule,
        MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule,
        MatSnackBarModule, MatProgressSpinnerModule, MatDividerModule
    ],
    templateUrl: './create.component.html',
})
export class CreateComponent {
    form: FormGroup;
    isDragging = false;
    uploadedFile: File | null = null;
    isSubmitting = false;

    constructor(
        private fb: FormBuilder,
        private router: Router,
        private examsService: ExamsService,
        private snackBar: MatSnackBar
    ) {
        this.form = this.fb.group({
            name: ['', [Validators.required, Validators.minLength(3)]]
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
        if (file.name.endsWith('.sql')) {
            this.uploadedFile = file;
        } else {
            this.snackBar.open('Please select a valid SQL script file (.sql)', 'Dismiss', {
                duration: 3000, panelClass: ['snack-error']
            });
        }
    }

    removeFile(): void {
        this.uploadedFile = null;
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
        this.isSubmitting = true;
        this.examsService.createExam({
            body: {
                ExamName: this.form.value.name,
                SqlFile: this.uploadedFile ?? undefined
            }
        }).subscribe({
            next: (exam) => {
                this.isSubmitting = false;
                this.snackBar.open(`Exam "${exam.examName}" created successfully!`, 'View', {
                    duration: 3000, panelClass: ['snack-success']
                });
                this.router.navigate(['/exams/data', exam.examId]);
            },
            error: (err) => {
                this.isSubmitting = false;
                const msg = err?.error?.detail || 'Failed to create exam. Please try again.';
                this.snackBar.open(msg, 'Dismiss', {
                    duration: 5000, panelClass: ['snack-error']
                });
            }
        });
    }

    onCancel(): void {
        this.router.navigate(['/exams']);
    }
}
