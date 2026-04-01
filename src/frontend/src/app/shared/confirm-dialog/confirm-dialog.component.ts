import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';

export interface ConfirmDialogData {
    title: string;
    message: string;
    confirmText?: string;
    cancelText?: string;
    confirmColor?: 'primary' | 'warn' | 'accent';
    icon?: string;
}

@Component({
    selector: 'app-confirm-dialog',
    standalone: true,
    imports: [CommonModule, MatButtonModule, MatDialogModule, MatIconModule],
    template: `
        <div class="flex flex-col gap-4 p-6 min-w-80 max-w-lg">
            <div class="flex items-center gap-3">
                <div class="flex h-12 w-12 shrink-0 items-center justify-center rounded-full"
                    [ngClass]="{
                        'bg-red-100 text-red-600': data.confirmColor === 'warn',
                        'bg-blue-100 text-blue-600': data.confirmColor === 'primary' || !data.confirmColor,
                        'bg-amber-100 text-amber-600': data.confirmColor === 'accent'
                    }">
                    <mat-icon [svgIcon]="data.icon || 'heroicons_outline:exclamation-triangle'" class="!h-6 !w-6"></mat-icon>
                </div>
                <div class="flex flex-col">
                    <h2 class="text-lg font-bold text-gray-900 dark:text-white">{{ data.title }}</h2>
                    <p class="text-sm text-gray-500 dark:text-gray-400 mt-0.5">{{ data.message }}</p>
                </div>
            </div>
            <div class="flex justify-end gap-3 pt-2">
                <button mat-stroked-button (click)="onCancel()">
                    {{ data.cancelText || 'Cancel' }}
                </button>
                <button mat-flat-button [color]="data.confirmColor || 'primary'" (click)="onConfirm()">
                    {{ data.confirmText || 'Confirm' }}
                </button>
            </div>
        </div>
    `
})
export class ConfirmDialogComponent {
    constructor(
        private _dialogRef: MatDialogRef<ConfirmDialogComponent>,
        @Inject(MAT_DIALOG_DATA) public data: ConfirmDialogData
    ) {}

    onConfirm(): void {
        this._dialogRef.close(true);
    }

    onCancel(): void {
        this._dialogRef.close(false);
    }
}
