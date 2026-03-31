import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { ApiConfiguration } from '../api/api-configuration';

import { TestResultRequest } from '../api/models';
import { apiTestrunnerGet, ApiTestrunnerGet$Params } from '../api/fn/test-results/api-testrunner-get';
import { apiTestrunnerStatusGet, ApiTestrunnerStatusGet$Params } from '../api/fn/test-results/api-testrunner-status-get';
import { apiTestresultsPost$Json, ApiTestresultsPost$Json$Params } from '../api/fn/test-results/api-testresults-post-json';

@Injectable({ providedIn: 'root' })
export class TestResultsService {
  constructor(private http: HttpClient, private config: ApiConfiguration) {}

  getTestRunner(params?: ApiTestrunnerGet$Params): Observable<void> {
    return apiTestrunnerGet(this.http, this.config.rootUrl, params).pipe(
      map(r => r.body as unknown as void),
      catchError(err => throwError(() => err))
    );
  }

  getTestRunnerStatus(params?: ApiTestrunnerStatusGet$Params): Observable<void> {
    return apiTestrunnerStatusGet(this.http, this.config.rootUrl, params).pipe(
      map(r => r.body as unknown as void),
      catchError(err => throwError(() => err))
    );
  }

  createTestResult(params: ApiTestresultsPost$Json$Params): Observable<TestResultRequest> {
    return apiTestresultsPost$Json(this.http, this.config.rootUrl, params).pipe(
      map(r => r.body),
      catchError(err => throwError(() => err))
    );
  }
}
