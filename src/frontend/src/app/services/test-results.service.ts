import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { ApiConfiguration } from '../api/api-configuration';

import { TestResultRequest } from '../api/models';

// POST /api/grading-runs
import { apiGradingRunsPost, ApiGradingRunsPost$Params } from '../api/fn/test-results/api-grading-runs-post';
// GET /api/grading-runs/current
import { apiGradingRunsCurrentGet, ApiGradingRunsCurrentGet$Params } from '../api/fn/test-results/api-grading-runs-current-get';
// POST /api/exams/{examId}/grading-runs
import { apiExamsExamIdGradingRunsPost, ApiExamsExamIdGradingRunsPost$Params } from '../api/fn/test-results/api-exams-exam-id-grading-runs-post';
// GET /api/testrunner
import { apiTestrunnerGet, ApiTestrunnerGet$Params } from '../api/fn/test-results/api-testrunner-get';
// GET /api/testrunner/status
import { apiTestrunnerStatusGet, ApiTestrunnerStatusGet$Params } from '../api/fn/test-results/api-testrunner-status-get';
// GET /api/exams/{examId}/testrunner
import { apiExamsExamIdTestrunnerGet, ApiExamsExamIdTestrunnerGet$Params } from '../api/fn/test-results/api-exams-exam-id-testrunner-get';
// POST /api/grading-results
import { apiGradingResultsPost$Json, ApiGradingResultsPost$Json$Params } from '../api/fn/test-results/api-grading-results-post-json';
// POST /api/testresults
import { apiTestresultsPost$Json, ApiTestresultsPost$Json$Params } from '../api/fn/test-results/api-testresults-post-json';

@Injectable({ providedIn: 'root' })
export class TestResultsService {
  constructor(private http: HttpClient, private config: ApiConfiguration) {}

  /** POST /api/grading-runs — Start a global grading run */
  startGradingRun(params: ApiGradingRunsPost$Params = {}): Observable<void> {
    return apiGradingRunsPost(this.http, this.config.rootUrl, params).pipe(
      map(r => r.body as unknown as void),
      catchError(err => throwError(() => err))
    );
  }

  /** GET /api/grading-runs/current — Get the current running grading job */
  getCurrentGradingRun(params: ApiGradingRunsCurrentGet$Params = {}): Observable<void> {
    return apiGradingRunsCurrentGet(this.http, this.config.rootUrl, params).pipe(
      map(r => r.body as unknown as void),
      catchError(err => throwError(() => err))
    );
  }

  /** POST /api/exams/{examId}/grading-runs — Start a grading run for a specific exam */
  startExamGradingRun(params: ApiExamsExamIdGradingRunsPost$Params): Observable<void> {
    return apiExamsExamIdGradingRunsPost(this.http, this.config.rootUrl, params).pipe(
      map(r => r.body as unknown as void),
      catchError(err => throwError(() => err))
    );
  }

  /** GET /api/testrunner — Get test runner status */
  getTestRunner(params: ApiTestrunnerGet$Params = {}): Observable<void> {
    return apiTestrunnerGet(this.http, this.config.rootUrl, params).pipe(
      map(r => r.body as unknown as void),
      catchError(err => throwError(() => err))
    );
  }

  /** GET /api/testrunner/status */
  getTestRunnerStatus(params: ApiTestrunnerStatusGet$Params = {}): Observable<void> {
    return apiTestrunnerStatusGet(this.http, this.config.rootUrl, params).pipe(
      map(r => r.body as unknown as void),
      catchError(err => throwError(() => err))
    );
  }

  /** GET /api/exams/{examId}/testrunner */
  getExamTestRunner(params: ApiExamsExamIdTestrunnerGet$Params): Observable<void> {
    return apiExamsExamIdTestrunnerGet(this.http, this.config.rootUrl, params).pipe(
      map(r => r.body as unknown as void),
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

  /** POST /api/testresults */
  submitTestResult(params: ApiTestresultsPost$Json$Params): Observable<TestResultRequest> {
    return apiTestresultsPost$Json(this.http, this.config.rootUrl, params).pipe(
      map(r => r.body),
      catchError(err => throwError(() => err))
    );
  }
}
