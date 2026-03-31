import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ExampleExamService } from './example-exam.service';
import { ExamDto } from '../api/models/exam-dto';

@Component({
  selector: 'app-example-exam',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="p-6 max-w-2xl mx-auto">
      <h2 class="text-2xl font-bold mb-4">Exam List (Example API Usage)</h2>
      
      <!-- Loading State -->
      <div *ngIf="loading" class="text-blue-600">Loading exams...</div>
      
      <!-- Error State -->
      <div *ngIf="error" class="bg-red-100 text-red-700 p-3 rounded mb-4">
        {{ error }}
      </div>
      
      <!-- Success State -->
      <ul *ngIf="!loading && !error && exams.length > 0" class="space-y-2">
        <li *ngFor="let exam of exams" class="border p-4 rounded shadow-sm">
          <strong>{{ exam.examName }}</strong> (ID: {{ exam.id }})
        </li>
      </ul>
      
      <!-- Empty State -->
      <div *ngIf="!loading && !error && exams.length === 0" class="text-gray-500">
        No exams found.
      </div>
    </div>
  `
})
export class ExampleExamComponent implements OnInit {
  exams: ExamDto[] = [];
  loading = false;
  error: string | null = null;

  constructor(private examService: ExampleExamService) {}

  ngOnInit(): void {
    this.loadExams();
  }

  loadExams(): void {
    this.loading = true;
    this.error = null;
    
    // Call the service using strong typing
    this.examService.getExams({ page: 1, pageSize: 10 }).subscribe({
      next: (result) => {
        // Handle successful response
        this.exams = result.items || [];
        this.loading = false;
      },
      error: (err) => {
        // Handle error
        this.error = err.message;
        this.loading = false;
      }
    });
  }
}
