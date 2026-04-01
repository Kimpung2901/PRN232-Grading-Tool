import { Component, Inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { SubmissionsService } from '../../../../../services/submissions.service';
import { SubmissionDto } from '../../../../../api/models';

@Component({
    selector: 'app-edit-submission-dialog',
    standalone: true,
    imports: [
        CommonModule,
        ReactiveFormsModule,
        MatButtonModule,
        MatDialogModule,
        MatFormFieldModule,
        MatInputModule,
        MatIconModule
    ],
    template: `
        <div class="flex flex-col max-w-160 min-w-80">
            <!-- Header -->
            <div class="flex items-center justify-between p-6 pb-0">
                <h2 class="text-2xl font-bold tracking-tight">Edit Submission</h2>
                <button mat-icon-button (click)="onCancel()" [disabled]="isLoading">
                    <mat-icon svgIcon="heroicons_outline:x-mark"></mat-icon>
                </button>
            </div>

            <form [formGroup]="editForm" (ngSubmit)="onSubmit()" class="flex flex-col p-6 space-y-4">
                <!-- Student Name -->
                <mat-form-field class="w-full" appearance="fill">
                    <mat-label>Student Name</mat-label>
                    <input matInput formControlName="studentName" placeholder="e.g. John Doe">
                    <mat-error *ngIf="editForm.get('studentName')?.hasError('required')">Student name is required</mat-error>
                </mat-form-field>

                <!-- Student Code -->
                <mat-form-field class="w-full" appearance="fill">
                    <mat-label>Student Code</mat-label>
                    <input matInput formControlName="studentCode" placeholder="e.g. HE123456">
                    <mat-error *ngIf="editForm.get('studentCode')?.hasError('required')">Student code is required</mat-error>
                </mat-form-field>

                <!-- Solution File -->
                <div class="flex flex-col space-y-2">
                    <label class="text-sm font-semibold text-gray-600 dark:text-gray-400">Update Solution File (.zip) (Optional)</label>
                    <div class="flex items-center gap-3">
                        <button type="button" mat-stroked-button (click)="fileInput.click()" [disabled]="isLoading">
                            <mat-icon svgIcon="heroicons_outline:cloud-arrow-up" class="mr-2"></mat-icon>
                            {{ selectedFile ? 'Change File' : 'Choose New Zip File' }}
                        </button>
                        <input #fileInput type="file" class="hidden" (change)="onFileSelected($event)" accept=".zip">
                        <span class="text-sm text-gray-500 truncate" *ngIf="selectedFile">{{ selectedFile.name }}</span>
                        <span class="text-xs text-indigo-500 italic" *ngIf="!selectedFile">Leave empty to keep existing file</span>
                    </div>
                </div>

                <!-- Footer Actions -->
                <div class="flex items-center justify-end gap-3 pt-4">
                    <button mat-button type="button" (click)="onCancel()" [disabled]="isLoading">Cancel</button>
                    <button mat-flat-button color="primary" type="submit" [disabled]="editForm.invalid || isLoading">
                        <span *ngIf="!isLoading">Save Changes</span>
                        <mat-icon *ngIf="isLoading" class="animate-spin" svgIcon="heroicons_outline:arrow-path"></mat-icon>
                    </button>
                </div>
            </form>
        </div>
    `
})
export class EditSubmissionDialogComponent implements OnInit {
    editForm: FormGroup;
    selectedFile: File | null = null;
    isLoading = false;

    constructor(
        private _formBuilder: FormBuilder,
        private _dialogRef: MatDialogRef<EditSubmissionDialogComponent>,
        private _submissionsService: SubmissionsService,
        @Inject(MAT_DIALOG_DATA) public data: { submission: SubmissionDto }
    ) {
        this.editForm = this._formBuilder.group({
            studentName: [data.submission.studentName, [Validators.required]],
            studentCode: [data.submission.studentCode, [Validators.required]]
        });
    }

    ngOnInit(): void {}

    onFileSelected(event: any): void {
        const file = event.target.files[0];
        if (file) {
            this.selectedFile = file;
        }
    }

    onCancel(): void {
        this._dialogRef.close();
    }

    onSubmit(): void {
        if (this.editForm.invalid) return;

        this.isLoading = true;
        const id = this.data.submission.id;
        if (!id) return;

        const body: any = {
            StudentName: this.editForm.get('studentName')?.value,
            StudentCode: this.editForm.get('studentCode')?.value
        };

        if (this.selectedFile) {
            body.File = this.selectedFile;
        }

        this._submissionsService.updateSubmission({ id, body }).subscribe({
            next: (updated) => {
                this.isLoading = false;
                this._dialogRef.close(updated);
            },
            error: (err) => {
                this.isLoading = false;
                console.error('Update failed', err);
            }
        });
    }
}
