import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject, Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { TestResultRequest } from '../api/models';

export interface RunnerStatus {
    isRunning: boolean;
    processId?: number | null;
    startedAtUtc?: string | null;
    lastCompletedAtUtc?: string | null;
    lastExitCode?: number | null;
    lastError?: string | null;
}

export interface RunnerCompletionLog {
    status: 'ok' | 'build false';
    output?: string;
    error?: string;
}

@Injectable({
    providedIn: 'root'
})
export class SignalRService {
    private hubConnection: signalR.HubConnection | null = null;
    
    // Subjects for components to subscribe to
    private testResultSubject = new Subject<TestResultRequest>();
    private statusSubject = new Subject<RunnerStatus>();
    private completionSubject = new Subject<RunnerCompletionLog>();

    // Exposed Observables
    testResult$: Observable<TestResultRequest> = this.testResultSubject.asObservable();
    status$: Observable<RunnerStatus> = this.statusSubject.asObservable();
    completion$: Observable<RunnerCompletionLog> = this.completionSubject.asObservable();

    constructor() {}

    public startConnection(): void {
        if (this.hubConnection && this.hubConnection.state === signalR.HubConnectionState.Connected) {
            return;
        }

        this.hubConnection = new signalR.HubConnectionBuilder()
            .withUrl(`${environment.apiUrl}/testHub`)
            .withAutomaticReconnect()
            .configureLogging(signalR.LogLevel.Information)
            .build();

        this.registerHandlers();

        this.hubConnection.onreconnecting(error => {
            console.warn('SignalR reconnecting', error);
        });

        this.hubConnection.onreconnected(connectionId => {
            console.log('SignalR reconnected', connectionId);
        });

        this.hubConnection.onclose(error => {
            console.error('SignalR connection closed', error);
        });

        this.hubConnection
            .start()
            .then(() => console.log('SignalR Connection Started'))
            .catch(err => console.error('Error while starting SignalR connection: ' + err));
    }

    public stopConnection(): void {
        if (this.hubConnection) {
            this.hubConnection.stop().then(() => console.log('SignalR Connection Stopped'));
        }
    }

    private registerHandlers(): void {
        if (!this.hubConnection) return;

        this.hubConnection.on('ReceiveTestResult', (data: TestResultRequest) => {
            this.testResultSubject.next(data);
        });

        this.hubConnection.on('RunnerStatusChanged', (data: RunnerStatus) => {
            this.statusSubject.next(data);
        });

        this.hubConnection.on('RunnerCompleted', (data: RunnerCompletionLog) => {
            this.completionSubject.next(data);
        });
    }
}
