import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { ApiConfiguration } from '../api/api-configuration';

import { SubmissionDto, SubmissionDtoPagedResult, SubmissionReportDto } from '../api/models';
import { apiExamsExamIdSubmissionsGet$Json, ApiExamsExamIdSubmissionsGet$Json$Params } from '../api/fn/submissions/api-exams-exam-id-submissions-get-json';
import { apiExamsExamIdSubmissionsPost$Json, ApiExamsExamIdSubmissionsPost$Json$Params } from '../api/fn/submissions/api-exams-exam-id-submissions-post-json';
import { apiSubmissionsIdGet$Json, ApiSubmissionsIdGet$Json$Params } from '../api/fn/submissions/api-submissions-id-get-json';
import { apiSubmissionsIdPatch$Json, ApiSubmissionsIdPatch$Json$Params } from '../api/fn/submissions/api-submissions-id-patch-json';
import { apiSubmissionsIdDelete, ApiSubmissionsIdDelete$Params } from '../api/fn/submissions/api-submissions-id-delete';

import { apiExamsExamIdGradingResultsGet$Json, ApiExamsExamIdGradingResultsGet$Json$Params } from '../api/fn/submissions/api-exams-exam-id-grading-results-get-json';
import { apiExamsExamIdSubmissionReportsGet$Json, ApiExamsExamIdSubmissionReportsGet$Json$Params } from '../api/fn/submissions/api-exams-exam-id-submission-reports-get-json';
import { apiSubmissionsIdRegradeRequestsPost$Json, ApiSubmissionsIdRegradeRequestsPost$Json$Params } from '../api/fn/submissions/api-submissions-id-regrade-requests-post-json';
import { apiSubmissionsIdRequeuePost$Json, ApiSubmissionsIdRequeuePost$Json$Params } from '../api/fn/submissions/api-submissions-id-requeue-post-json';
import { apiExamsExamIdRegradeRequestsPost, ApiExamsExamIdRegradeRequestsPost$Params } from '../api/fn/submissions/api-exams-exam-id-regrade-requests-post';
import { apiExamsExamIdSubmissionsRequeuePost, ApiExamsExamIdSubmissionsRequeuePost$Params } from '../api/fn/submissions/api-exams-exam-id-submissions-requeue-post';

@Injectable({ providedIn: 'root' })
export class SubmissionsService {
  constructor(private http: HttpClient, private config: ApiConfiguration) {}

  getSubmissions(params: ApiExamsExamIdSubmissionsGet$Json$Params): Observable<SubmissionDtoPagedResult> {
    return apiExamsExamIdSubmissionsGet$Json(this.http, this.config.rootUrl, params).pipe(
      map(r => r.body),
      catchError(err => throwError(() => err))
    );
  }

  createSubmission(params: ApiExamsExamIdSubmissionsPost$Json$Params): Observable<SubmissionDto> {
    return apiExamsExamIdSubmissionsPost$Json(this.http, this.config.rootUrl, params).pipe(
      map(r => r.body),
      catchError(err => throwError(() => err))
    );
  }

  getSubmissionById(params: ApiSubmissionsIdGet$Json$Params): Observable<SubmissionDto> {
    return apiSubmissionsIdGet$Json(this.http, this.config.rootUrl, params).pipe(
      map(r => r.body),
      catchError(err => throwError(() => err))
    );
  }

  updateSubmission(params: ApiSubmissionsIdPatch$Json$Params): Observable<SubmissionDto> {
    return apiSubmissionsIdPatch$Json(this.http, this.config.rootUrl, params).pipe(
      map(r => r.body),
      catchError(err => throwError(() => err))
    );
  }

  deleteSubmission(params: ApiSubmissionsIdDelete$Params): Observable<void> {
    return apiSubmissionsIdDelete(this.http, this.config.rootUrl, params).pipe(
      map(r => r.body as unknown as void),
      catchError(err => throwError(() => err))
    );
  }

  getGradingResults(params: ApiExamsExamIdGradingResultsGet$Json$Params): Observable<Array<SubmissionReportDto>> {
    return apiExamsExamIdGradingResultsGet$Json(this.http, this.config.rootUrl, params).pipe(
      map(r => r.body),
      catchError(err => throwError(() => err))
    );
  }

  getSubmissionReports(params: ApiExamsExamIdSubmissionReportsGet$Json$Params): Observable<Array<SubmissionReportDto>> {
    return apiExamsExamIdSubmissionReportsGet$Json(this.http, this.config.rootUrl, params).pipe(
      map(r => r.body),
      catchError(err => throwError(() => err))
    );
  }

  regradeRequest(params: ApiSubmissionsIdRegradeRequestsPost$Json$Params): Observable<SubmissionDto> {
    return apiSubmissionsIdRegradeRequestsPost$Json(this.http, this.config.rootUrl, params).pipe(
      map(r => r.body),
      catchError(err => throwError(() => err))
    );
  }

  requeueSubmission(params: ApiSubmissionsIdRequeuePost$Json$Params): Observable<SubmissionDto> {
    return apiSubmissionsIdRequeuePost$Json(this.http, this.config.rootUrl, params).pipe(
      map(r => r.body),
      catchError(err => throwError(() => err))
    );
  }

  regradeExamRequests(params: ApiExamsExamIdRegradeRequestsPost$Params): Observable<void> {
    return apiExamsExamIdRegradeRequestsPost(this.http, this.config.rootUrl, params).pipe(
      map(r => r.body as unknown as void),
      catchError(err => throwError(() => err))
    );
  }

  requeueExamSubmissions(params: ApiExamsExamIdSubmissionsRequeuePost$Params): Observable<void> {
    return apiExamsExamIdSubmissionsRequeuePost(this.http, this.config.rootUrl, params).pipe(
      map(r => r.body as unknown as void),
      catchError(err => throwError(() => err))
    );
  }
}
