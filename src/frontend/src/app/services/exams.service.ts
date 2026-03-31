import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { ApiConfiguration } from '../api/api-configuration';

import { ExamDto, ExamDtoPagedResult } from '../api/models';
import { apiExamsGet$Json, ApiExamsGet$Json$Params } from '../api/fn/exams/api-exams-get-json';
import { apiExamsPost$Json, ApiExamsPost$Json$Params } from '../api/fn/exams/api-exams-post-json';
import { apiExamsExamIdGet$Json, ApiExamsExamIdGet$Json$Params } from '../api/fn/exams/api-exams-exam-id-get-json';
import { apiExamsExamIdPatch$Json, ApiExamsExamIdPatch$Json$Params } from '../api/fn/exams/api-exams-exam-id-patch-json';
import { apiExamsExamIdDelete, ApiExamsExamIdDelete$Params } from '../api/fn/exams/api-exams-exam-id-delete';

@Injectable({ providedIn: 'root' })
export class ExamsService {
  constructor(private http: HttpClient, private config: ApiConfiguration) {}

  getExams(params: ApiExamsGet$Json$Params = {}): Observable<ExamDtoPagedResult> {
    return apiExamsGet$Json(this.http, this.config.rootUrl, params).pipe(
      map(r => r.body),
      catchError(err => throwError(() => err))
    );
  }

  createExam(params: ApiExamsPost$Json$Params): Observable<ExamDto> {
    return apiExamsPost$Json(this.http, this.config.rootUrl, params).pipe(
      map(r => r.body),
      catchError(err => throwError(() => err))
    );
  }

  getExamById(params: ApiExamsExamIdGet$Json$Params): Observable<ExamDto> {
    return apiExamsExamIdGet$Json(this.http, this.config.rootUrl, params).pipe(
      map(r => r.body),
      catchError(err => throwError(() => err))
    );
  }

  updateExam(params: ApiExamsExamIdPatch$Json$Params): Observable<ExamDto> {
    return apiExamsExamIdPatch$Json(this.http, this.config.rootUrl, params).pipe(
      map(r => r.body),
      catchError(err => throwError(() => err))
    );
  }

  deleteExam(params: ApiExamsExamIdDelete$Params): Observable<void> {
    return apiExamsExamIdDelete(this.http, this.config.rootUrl, params).pipe(
      map(r => r.body as unknown as void),
      catchError(err => throwError(() => err))
    );
  }
}
