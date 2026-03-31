import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, RouterModule, Router } from '@angular/router';
import { ExamsService } from '../../../../services/exams.service';
import { ExamDto } from '../../../../api/models';

@Component({
    selector: 'app-exams-detail',
    standalone: true,
    imports: [CommonModule, RouterModule],
    templateUrl: './detail.component.html',
})
export class DetailComponent implements OnInit {
    id = '';
    detail: ExamDto | null = null;
    activeTab = 'overview'; // overview, test-cases, data

    constructor(
        private route: ActivatedRoute, 
        private examsService: ExamsService,
        private router: Router
    ) {
        this.id = this.route.snapshot.paramMap.get('id') ?? '';
    }

    ngOnInit(): void {
        this.loadDetail();
    }

    loadDetail(): void {
        const examIdNum = parseInt(this.id, 10);
        if (isNaN(examIdNum)) return;
        
        this.examsService.getExamById({ examId: examIdNum }).subscribe({
            next: (res) => this.detail = res,
            error: (err) => console.error('Error fetching details', err)
        });
    }
}
