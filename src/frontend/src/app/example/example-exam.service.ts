import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { ApiConfiguration } from '../api/api-configuration';
import { apiExamsGet$Json, ApiExamsGet$Json$Params } from '../api/fn/exams/api-exams-get-json';
import { ExamDtoPagedResult } from '../api/models/exam-dto-paged-result';

// NOTE: In a real app, import 'environment' from '../environments/environment'
const environment = { apiUrl: 'http://localhost:5000' };

@Injectable({
  providedIn: 'root'
})
export class ExampleExamService {
  constructor(
    private http: HttpClient,
    private apiConfig: ApiConfiguration
  ) {
    // Configure base URL from environment
    this.apiConfig.rootUrl = environment.apiUrl;
  }

  /**
   * Fetches paginated exams with RxJS Observables and error handling
   * @param params Query parameters for the request
   * @returns Observable of paginated exam results
   */
  getExams(params: ApiExamsGet$Json$Params): Observable<ExamDtoPagedResult> {
    return apiExamsGet$Json(this.http, this.apiConfig.rootUrl, params).pipe(
      map(response => response.body),
      catchError(error => {
        console.error('ExampleExamService: Failed to fetch exams', error);
        // Handle error and re-throw
        return throwError(() => new Error('Error occurred while fetching exams.'));
      })
    );
  }
}
