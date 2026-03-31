import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import {
    FormBuilder,
    FormGroup,
    FormsModule,
    ReactiveFormsModule,
    Validators,
} from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';

@Component({
    selector: 'app-exams-create',
    standalone: true,
    imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterModule, MatFormFieldModule, MatSelectModule],
    templateUrl: './create.component.html',
})
export class CreateComponent {
    form: FormGroup;
    isDragging = false;
    uploadedFile: File | null = null;
    semesters = ['Spring 2026', 'Fall 2025', 'Summer 2025', 'Spring 2025'];

    constructor(
        private fb: FormBuilder,
        private router: Router
    ) {
        this.form = this.fb.group({
            name: ['', [Validators.required, Validators.minLength(3)]],
            semester: ['', Validators.required],
            description: ['']
        });
    }

    // Drag & Drop for Postman Collection File
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
        // Here we could add validation to check if it's a .json file
        this.uploadedFile = file;
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
        if (this.form.valid) {
            if (!this.uploadedFile) {
                alert('Please upload a Postman Collection File.');
                return;
            }
            console.log('Submit Exam:', this.form.value);
            console.log('Collection File:', this.uploadedFile.name);
            // Integrate API call here to save the exam
            
            this.router.navigate(['/exams/dashboard']);
        } else {
            this.form.markAllAsTouched();
        }
    }

    onCancel(): void {
        this.router.navigate(['/exams/dashboard']);
    }

    backToExams(): void {
        this.router.navigate(['/exams/dashboard']);
    }
}
