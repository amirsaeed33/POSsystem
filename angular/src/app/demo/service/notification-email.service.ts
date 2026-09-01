import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';

export interface SendNotificationEmailInput {
    branchId?: number | null;
    targetEmail?: string;
}

@Injectable({
    providedIn: 'root'
})
export class NotificationEmailService {
    private baseUrl = `${environment.apiUrl}/api/services/app/NotificationEmail`;

    constructor(private http: HttpClient) {}

    sendLowStockReport(input: SendNotificationEmailInput): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/SendLowStockReport`, input);
    }

    sendDailyBusinessSummary(input: SendNotificationEmailInput): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/SendDailyBusinessSummary`, input);
    }
}
