import { CommonModule } from '@angular/common';
import { Component, ElementRef, ViewChild } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { RouterModule, Router } from '@angular/router';

@Component({
    selector: 'app-submissions-create',
    standalone: true,
    imports: [
        CommonModule,
        ReactiveFormsModule,
        RouterModule,
        MatButtonModule,
        MatIconModule,
        MatFormFieldModule,
        MatInputModule,
        MatSelectModule
    ],
    templateUrl: './create.component.html'
})
export class CreateComponent {
    submissionForm: FormGroup;
    selectedFile: File | null = null;
    isDragging = false;
    currentDate = new Date().toLocaleString();
    
    @ViewChild('fileInput') fileInput!: ElementRef;

    exams = [
        { id: 'EXM-101', name: 'Midterm PRN232 - Spring 2026' },
        { id: 'EXM-102', name: 'Final PRN232 - Spring 2026' }
    ];

    constructor(
        private fb: FormBuilder,
        private router: Router
    ) {
        this.submissionForm = this.fb.group({
            studentId: ['', [Validators.required]],
            examId: ['', [Validators.required]]
        });

        // Update current date every minute
        setInterval(() => {
            this.currentDate = new Date().toLocaleString();
        }, 60000);
    }

    onDragOver(event: DragEvent): void {
        event.preventDefault();
        event.stopPropagation();
        this.isDragging = true;
    }

    onDragLeave(event: DragEvent): void {
        event.preventDefault();
        event.stopPropagation();
        this.isDragging = false;
    }

    onDrop(event: DragEvent): void {
        event.preventDefault();
        event.stopPropagation();
        this.isDragging = false;
        
        const files = event.dataTransfer?.files;
        if (files && files.length > 0) {
            this.handleFile(files[0]);
        }
    }

    onFileSelected(event: Event): void {
        const input = event.target as HTMLInputElement;
        if (input.files && input.files.length > 0) {
            this.handleFile(input.files[0]);
        }
    }

    handleFile(file: File): void {
        // Only accept zip/rar/7z
        const validTypes = ['.zip', '.rar', '.7z'];
        const isValid = validTypes.some(ext => file.name.toLowerCase().endsWith(ext));
        
        if (isValid) {
            this.selectedFile = file;
        } else {
            alert('Please upload a valid archive file (.zip, .rar, .7z)');
        }
    }

    removeFile(): void {
        this.selectedFile = null;
        if (this.fileInput) {
            this.fileInput.nativeElement.value = '';
        }
    }

    getFileSize(size: number): string {
        return (size / (1024 * 1024)).toFixed(2) + ' MB';
    }

    onSubmit(): void {
        if (this.submissionForm.valid && this.selectedFile) {
            console.log('Form Data:', this.submissionForm.value);
            console.log('File:', this.selectedFile);
            console.log('Submitted At:', new Date().toISOString());
            
            // Navigate back to dashboard after submit (simulated)
            setTimeout(() => {
                this.router.navigate(['/submissions', 'dashboard']);
            }, 500);
        } else {
            this.submissionForm.markAllAsTouched();
            if (!this.selectedFile) {
                alert('Please provide a submission file.');
            }
        }
    }
}
