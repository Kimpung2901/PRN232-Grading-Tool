import { CommonModule } from '@angular/common';
import { Component, ElementRef, ViewChild } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { RouterModule, Router } from '@angular/router';
import { ExamsService } from '../../../../services/exams.service';
import { SubmissionsService } from '../../../../services/submissions.service';
import { ExamDto } from '../../../../api/models';

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
    
    exams: ExamDto[] = [];

    @ViewChild('projectFileInput') projectFileInput!: ElementRef;

    constructor(
        private fb: FormBuilder,
        private router: Router,
        private examsService: ExamsService,
        private submissionsService: SubmissionsService
    ) {
        this.submissionForm = this.fb.group({
            studentName: ['', [Validators.required]],
            studentCode: ['', [Validators.required]],
            examId: ['', [Validators.required]]
        });

        this.loadExams();
    }

    loadExams(): void {
        this.examsService.getExams({ pageSize: 100 }).subscribe({
            next: (res) => this.exams = res.items || [],
            error: (err) => console.error('Failed to load exams', err)
        });
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
        if (this.projectFileInput) {
            this.projectFileInput.nativeElement.value = '';
        }
    }

    getFileSize(size: number): string {
        return (size / (1024 * 1024)).toFixed(2) + ' MB';
    }

    onSubmit(): void {
        if (this.submissionForm.valid && this.selectedFile) {
            const val = this.submissionForm.value;
            this.submissionsService.createSubmission({
                examId: parseInt(val.examId, 10),
                body: {
                    StudentName: val.studentName,
                    StudentCode: val.studentCode,
                    File: this.selectedFile
                }
            }).subscribe({
                next: () => this.router.navigate(['/submissions', 'dashboard']),
                error: (err) => console.error('Submission failed', err)
            });
        } else {
            this.submissionForm.markAllAsTouched();
            if (!this.selectedFile) {
                alert('Please provide a submission file.');
            }
        }
    }
}

