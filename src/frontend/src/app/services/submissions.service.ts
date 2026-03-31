import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { ApiConfiguration } from '../api/api-configuration';

import { SubmissionDto, SubmissionDtoPagedResult } from '../api/models';
import { apiExamsExamIdSubmissionsGet$Json, ApiExamsExamIdSubmissionsGet$Json$Params } from '../api/fn/submissions/api-exams-exam-id-submissions-get-json';
import { apiExamsExamIdSubmissionsPost$Json, ApiExamsExamIdSubmissionsPost$Json$Params } from '../api/fn/submissions/api-exams-exam-id-submissions-post-json';
import { apiSubmissionsIdGet$Json, ApiSubmissionsIdGet$Json$Params } from '../api/fn/submissions/api-submissions-id-get-json';
import { apiSubmissionsIdPatch$Json, ApiSubmissionsIdPatch$Json$Params } from '../api/fn/submissions/api-submissions-id-patch-json';
import { apiSubmissionsIdDelete, ApiSubmissionsIdDelete$Params } from '../api/fn/submissions/api-submissions-id-delete';

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
}
