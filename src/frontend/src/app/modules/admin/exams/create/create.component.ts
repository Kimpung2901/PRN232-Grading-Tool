import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { ExamsService } from '../../../../services/exams.service';

@Component({
    selector: 'app-exams-create',
    standalone: true,
    imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterModule],
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
        private examsService: ExamsService
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
            alert('Please select a valid SQL script file (.sql)');
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
        if (this.form.valid && this.uploadedFile) {
            this.isSubmitting = true;
            this.examsService.createExam({
                body: {
                    ExamName: this.form.value.name,
                    SqlFile: this.uploadedFile
                }
            }).subscribe({
                next: () => {
                    this.isSubmitting = false;
                    this.router.navigate(['/exams']);
                },
                error: (err) => {
                    this.isSubmitting = false;
                    console.error('Error creating exam', err);
                }
            });
        } else if (!this.uploadedFile) {
            alert('Please upload an SQL Initialization Script.');
        } else {
            this.form.markAllAsTouched();
        }
    }

    onCancel(): void {
        this.router.navigate(['/exams']);
    }
}
