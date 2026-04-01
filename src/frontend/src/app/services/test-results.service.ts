import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { ApiConfiguration } from '../api/api-configuration';

import { TestResultRequest } from '../api/models';
import { RunnerStatus } from './signalr.service';

// POST /api/grading-runs
import { apiGradingRunsPost, ApiGradingRunsPost$Params } from '../api/fn/test-results/api-grading-runs-post';
// GET /api/grading-runs/current
import { apiGradingRunsCurrentGet, ApiGradingRunsCurrentGet$Params } from '../api/fn/test-results/api-grading-runs-current-get';
// POST /api/exams/{examId}/grading-runs
import { apiExamsExamIdGradingRunsPost, ApiExamsExamIdGradingRunsPost$Params } from '../api/fn/test-results/api-exams-exam-id-grading-runs-post';
// POST /api/grading-results
import { apiGradingResultsPost$Json, ApiGradingResultsPost$Json$Params } from '../api/fn/test-results/api-grading-results-post-json';

@Injectable({ providedIn: 'root' })
export class TestResultsService {
  constructor(private http: HttpClient, private config: ApiConfiguration) {}

  /** POST /api/grading-runs — Start a global grading run */
  startGradingRun(params: ApiGradingRunsPost$Params = {}): Observable<{ message: string }> {
    return apiGradingRunsPost(this.http, this.config.rootUrl, params).pipe(
      map(r => r.body as unknown as { message: string }),
      catchError(err => throwError(() => err))
    );
  }

  /** GET /api/grading-runs/current — Get the current running grading job */
  getCurrentGradingRun(params: ApiGradingRunsCurrentGet$Params = {}): Observable<RunnerStatus> {
    // The generated helper returns void, so we fetch directly with the right type
    return this.http.get<RunnerStatus>(`${this.config.rootUrl}/api/grading-runs/current`).pipe(
      catchError(err => throwError(() => err))
    );
  }

  /** POST /api/exams/{examId}/grading-runs — Start a grading run for a specific exam */
  startExamGradingRun(params: ApiExamsExamIdGradingRunsPost$Params): Observable<{ message: string }> {
    return apiExamsExamIdGradingRunsPost(this.http, this.config.rootUrl, params).pipe(
      map(r => r.body as unknown as { message: string }),
      catchError(err => throwError(() => err))
    );
  }

  /** POST /api/grading-results */
  submitGradingResult(params: ApiGradingResultsPost$Json$Params): Observable<TestResultRequest> {
    return apiGradingResultsPost$Json(this.http, this.config.rootUrl, params).pipe(
      map(r => r.body),
      catchError(err => throwError(() => err))
    );
  }
}
