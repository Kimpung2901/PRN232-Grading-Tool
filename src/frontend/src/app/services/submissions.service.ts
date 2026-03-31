import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { ApiConfiguration } from '../api/api-configuration';

import { SubmissionDto, SubmissionDtoPagedResult, SubmissionReportDto } from '../api/models';

// GET /api/exams/{examId}/submissions
import { apiExamsExamIdSubmissionsGet$Json, ApiExamsExamIdSubmissionsGet$Json$Params } from '../api/fn/submissions/api-exams-exam-id-submissions-get-json';
// POST /api/exams/{examId}/submissions
import { apiExamsExamIdSubmissionsPost$Json, ApiExamsExamIdSubmissionsPost$Json$Params } from '../api/fn/submissions/api-exams-exam-id-submissions-post-json';
// GET /api/submissions/{id}
import { apiSubmissionsIdGet$Json, ApiSubmissionsIdGet$Json$Params } from '../api/fn/submissions/api-submissions-id-get-json';
// PATCH /api/submissions/{id}
import { apiSubmissionsIdPatch$Json, ApiSubmissionsIdPatch$Json$Params } from '../api/fn/submissions/api-submissions-id-patch-json';
// DELETE /api/submissions/{id}
import { apiSubmissionsIdDelete, ApiSubmissionsIdDelete$Params } from '../api/fn/submissions/api-submissions-id-delete';
// POST /api/submissions/{id}/requeue
import { apiSubmissionsIdRequeuePost$Json, ApiSubmissionsIdRequeuePost$Json$Params } from '../api/fn/submissions/api-submissions-id-requeue-post-json';
// POST /api/submissions/{id}/regrade-requests
import { apiSubmissionsIdRegradeRequestsPost$Json, ApiSubmissionsIdRegradeRequestsPost$Json$Params } from '../api/fn/submissions/api-submissions-id-regrade-requests-post-json';
// POST /api/exams/{examId}/regrade-requests
import { apiExamsExamIdRegradeRequestsPost, ApiExamsExamIdRegradeRequestsPost$Params } from '../api/fn/submissions/api-exams-exam-id-regrade-requests-post';
// POST /api/exams/{examId}/submissions/requeue
import { apiExamsExamIdSubmissionsRequeuePost, ApiExamsExamIdSubmissionsRequeuePost$Params } from '../api/fn/submissions/api-exams-exam-id-submissions-requeue-post';
// GET /api/exams/{examId}/grading-results
import { apiExamsExamIdGradingResultsGet$Json, ApiExamsExamIdGradingResultsGet$Json$Params } from '../api/fn/submissions/api-exams-exam-id-grading-results-get-json';
// GET /api/exams/{examId}/submission-reports
import { apiExamsExamIdSubmissionReportsGet$Json, ApiExamsExamIdSubmissionReportsGet$Json$Params } from '../api/fn/submissions/api-exams-exam-id-submission-reports-get-json';

@Injectable({ providedIn: 'root' })
export class SubmissionsService {
  constructor(private http: HttpClient, private config: ApiConfiguration) {}

  /** GET /api/exams/{examId}/submissions */
  getSubmissions(params: ApiExamsExamIdSubmissionsGet$Json$Params): Observable<SubmissionDtoPagedResult> {
    return apiExamsExamIdSubmissionsGet$Json(this.http, this.config.rootUrl, params).pipe(
      map(r => r.body),
      catchError(err => throwError(() => err))
    );
  }

  /** POST /api/exams/{examId}/submissions */
  createSubmission(params: ApiExamsExamIdSubmissionsPost$Json$Params): Observable<SubmissionDto> {
    return apiExamsExamIdSubmissionsPost$Json(this.http, this.config.rootUrl, params).pipe(
      map(r => r.body),
      catchError(err => throwError(() => err))
    );
  }

  /** GET /api/submissions/{id} */
  getSubmissionById(params: ApiSubmissionsIdGet$Json$Params): Observable<SubmissionDto> {
    return apiSubmissionsIdGet$Json(this.http, this.config.rootUrl, params).pipe(
      map(r => r.body),
      catchError(err => throwError(() => err))
    );
  }

  /** PATCH /api/submissions/{id} */
  updateSubmission(params: ApiSubmissionsIdPatch$Json$Params): Observable<SubmissionDto> {
    return apiSubmissionsIdPatch$Json(this.http, this.config.rootUrl, params).pipe(
      map(r => r.body),
      catchError(err => throwError(() => err))
    );
  }

  /** DELETE /api/submissions/{id} */
  deleteSubmission(params: ApiSubmissionsIdDelete$Params): Observable<void> {
    return apiSubmissionsIdDelete(this.http, this.config.rootUrl, params).pipe(
      map(r => r.body as unknown as void),
      catchError(err => throwError(() => err))
    );
  }

  /** POST /api/submissions/{id}/requeue */
  requeueSubmission(params: ApiSubmissionsIdRequeuePost$Json$Params): Observable<SubmissionDto> {
    return apiSubmissionsIdRequeuePost$Json(this.http, this.config.rootUrl, params).pipe(
      map(r => r.body),
      catchError(err => throwError(() => err))
    );
  }

  /** POST /api/submissions/{id}/regrade-requests */
  regradeSubmission(params: ApiSubmissionsIdRegradeRequestsPost$Json$Params): Observable<SubmissionDto> {
    return apiSubmissionsIdRegradeRequestsPost$Json(this.http, this.config.rootUrl, params).pipe(
      map(r => r.body),
      catchError(err => throwError(() => err))
    );
  }

  /** POST /api/exams/{examId}/regrade-requests — regrade all submissions in an exam */
  requeueExamSubmissions(params: ApiExamsExamIdRegradeRequestsPost$Params): Observable<void> {
    return apiExamsExamIdRegradeRequestsPost(this.http, this.config.rootUrl, params).pipe(
      map(r => r.body as unknown as void),
      catchError(err => throwError(() => err))
    );
  }

  /** POST /api/exams/{examId}/submissions/requeue */
  requeueAllSubmissions(params: ApiExamsExamIdSubmissionsRequeuePost$Params): Observable<void> {
    return apiExamsExamIdSubmissionsRequeuePost(this.http, this.config.rootUrl, params).pipe(
      map(r => r.body as unknown as void),
      catchError(err => throwError(() => err))
    );
  }

  /** GET /api/exams/{examId}/grading-results */
  getGradingResults(params: ApiExamsExamIdGradingResultsGet$Json$Params): Observable<SubmissionReportDto[]> {
    return apiExamsExamIdGradingResultsGet$Json(this.http, this.config.rootUrl, params).pipe(
      map(r => r.body),
      catchError(err => throwError(() => err))
    );
  }

  /** GET /api/exams/{examId}/submission-reports */
  getSubmissionReports(params: ApiExamsExamIdSubmissionReportsGet$Json$Params): Observable<SubmissionReportDto[]> {
    return apiExamsExamIdSubmissionReportsGet$Json(this.http, this.config.rootUrl, params).pipe(
      map(r => r.body),
      catchError(err => throwError(() => err))
    );
  }
}
