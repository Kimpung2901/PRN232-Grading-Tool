import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { ApiConfiguration } from '../api/api-configuration';

import { TestCaseDto, TestCaseDtoPagedResult } from '../api/models';
import { apiExamsExamIdTestcasesGet$Json, ApiExamsExamIdTestcasesGet$Json$Params } from '../api/fn/test-cases/api-exams-exam-id-testcases-get-json';
import { apiExamsExamIdTestcasesPost$Json, ApiExamsExamIdTestcasesPost$Json$Params } from '../api/fn/test-cases/api-exams-exam-id-testcases-post-json';
import { apiExamsExamIdTestcasesIdGet$Json, ApiExamsExamIdTestcasesIdGet$Json$Params } from '../api/fn/test-cases/api-exams-exam-id-testcases-id-get-json';
import { apiExamsExamIdTestcasesIdPatch$Json, ApiExamsExamIdTestcasesIdPatch$Json$Params } from '../api/fn/test-cases/api-exams-exam-id-testcases-id-patch-json';
import { apiExamsExamIdTestcasesIdDelete, ApiExamsExamIdTestcasesIdDelete$Params } from '../api/fn/test-cases/api-exams-exam-id-testcases-id-delete';

@Injectable({ providedIn: 'root' })
export class TestCasesService {
  constructor(private http: HttpClient, private config: ApiConfiguration) {}

  getTestCases(params: ApiExamsExamIdTestcasesGet$Json$Params): Observable<TestCaseDtoPagedResult> {
    return apiExamsExamIdTestcasesGet$Json(this.http, this.config.rootUrl, params).pipe(
      map(r => r.body),
      catchError(err => throwError(() => err))
    );
  }

  createTestCase(params: ApiExamsExamIdTestcasesPost$Json$Params): Observable<TestCaseDto> {
    return apiExamsExamIdTestcasesPost$Json(this.http, this.config.rootUrl, params).pipe(
      map(r => r.body),
      catchError(err => throwError(() => err))
    );
  }

  getTestCaseById(params: ApiExamsExamIdTestcasesIdGet$Json$Params): Observable<TestCaseDto> {
    return apiExamsExamIdTestcasesIdGet$Json(this.http, this.config.rootUrl, params).pipe(
      map(r => r.body),
      catchError(err => throwError(() => err))
    );
  }

  updateTestCase(params: ApiExamsExamIdTestcasesIdPatch$Json$Params): Observable<TestCaseDto> {
    return apiExamsExamIdTestcasesIdPatch$Json(this.http, this.config.rootUrl, params).pipe(
      map(r => r.body),
      catchError(err => throwError(() => err))
    );
  }

  deleteTestCase(params: ApiExamsExamIdTestcasesIdDelete$Params): Observable<void> {
    return apiExamsExamIdTestcasesIdDelete(this.http, this.config.rootUrl, params).pipe(
      map(r => r.body as unknown as void),
      catchError(err => throwError(() => err))
    );
  }
}
