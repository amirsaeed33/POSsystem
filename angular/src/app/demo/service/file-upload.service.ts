import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';

@Injectable({
    providedIn: 'root',
})
export class FileUploadService {
    private apiUrl = `${environment.apiUrl}/api/FileUpload`;

    constructor(
        private http: HttpClient,
        private authService: AuthService
    ) { }

    uploadProfilePicture(file: File): Promise<string> {
        const headers = this.getAuthHeaders();
        const formData = new FormData();
        formData.append('file', file);

        return this.http.post<any>(`${this.apiUrl}/upload-profile-picture`, formData, { headers })
            .toPromise()
            .then((res: any) => {
                if (!res) {
                    throw new Error('No response from server');
                }

                // Check for ABP error response
                if (res.error || (res.success === false)) {
                    const errorMessage = res.error?.message || res.error?.details || 'Failed to upload file';
                    throw new Error(errorMessage);
                }

                // Extract URL from response
                const url = res.url || res.result?.url;
                if (!url) {
                    throw new Error('Invalid response: URL not found');
                }

                return url;
            })
            .catch((error: any) => {
                if (error?.error) {
                    const abpError = error.error;
                    const errorMessage = abpError.error?.message ||
                                       abpError.message ||
                                       abpError.details ||
                                       error.message ||
                                       'Failed to upload profile picture';
                    throw new Error(errorMessage);
                }
                throw error;
            });
    }

    deleteProfilePicture(fileName: string): Promise<void> {
        const headers = this.getAuthHeaders();
        return this.http.delete<any>(`${this.apiUrl}/delete-profile-picture?fileName=${fileName}`, { headers })
            .toPromise()
            .then(() => {})
            .catch((error: any) => {
                if (error?.error) {
                    const abpError = error.error;
                    const errorMessage = abpError.error?.message ||
                                       abpError.message ||
                                       abpError.details ||
                                       error.message ||
                                       'Failed to delete profile picture';
                    throw new Error(errorMessage);
                }
                throw error;
            });
    }

    private getAuthHeaders(): HttpHeaders {
        const token = this.authService.getAccessToken();
        let headers = new HttpHeaders();
        if (token) {
            headers = headers.set('Authorization', `Bearer ${token}`);
        }
        return headers;
    }
}

