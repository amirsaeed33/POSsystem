import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from 'src/environments/environment';
import {
    PagedSellerResultRequestDto,
    PublicSellerDto,
    RegisterSellerDto,
    RejectSellerDto,
    SellerDto,
    SellerStatus,
    SellerStatuses,
} from '../api/seller';

@Injectable({ providedIn: 'root' })
export class SellerService {
    private readonly apiUrl = `${environment.apiUrl}/api/services/app/Seller`;

    constructor(private http: HttpClient) {}

    async register(input: RegisterSellerDto): Promise<SellerDto> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/Register`, input)
        );
        return this.mapDto(this.unwrap(res, 'Failed to submit seller application'));
    }

    async getPublicSellers(): Promise<PublicSellerDto[]> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetPublicSellers`)
        );
        const result = this.unwrap(res, 'Failed to load sellers');
        const items = Array.isArray(result) ? result : result?.items || [];
        return (Array.isArray(items) ? items : []).map((x: any) => this.mapPublic(x));
    }

    async getAll(
        input: PagedSellerResultRequestDto = {}
    ): Promise<{ totalCount: number; items: SellerDto[] }> {
        const params: any = {
            MaxResultCount: input.maxResultCount ?? 200,
            SkipCount: input.skipCount ?? 0,
        };
        if (input.keyword?.trim()) {
            params.Keyword = input.keyword.trim();
        }
        if (input.status != null) {
            params.Status = input.status;
        }
        if (input.sorting) {
            params.Sorting = input.sorting;
        }

        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetAll`, { params })
        );
        const result = this.unwrap(res, 'Failed to load sellers');
        const items = (result?.items || []).map((x: any) => this.mapDto(x));
        return { totalCount: result?.totalCount ?? items.length, items };
    }

    async get(id: number): Promise<SellerDto> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/Get`, { params: { Id: id } })
        );
        return this.mapDto(this.unwrap(res, 'Failed to load seller'));
    }

    async approve(id: number): Promise<void> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/Approve`, { id })
        );
        if (res == null) {
            return;
        }
        this.unwrap(res, 'Failed to approve seller');
    }

    async reject(input: RejectSellerDto): Promise<void> {
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/Reject`, input)
        );
        if (res == null) {
            return;
        }
        this.unwrap(res, 'Failed to reject seller');
    }

    async delete(id: number): Promise<void> {
        const res: any = await firstValueFrom(
            this.http.delete<any>(`${this.apiUrl}/Delete`, { params: { Id: id } })
        );
        if (res == null) {
            return;
        }
        this.unwrap(res, 'Failed to delete seller');
    }

    statusLabel(status?: SellerStatus | null): string {
        switch (status) {
            case SellerStatuses.Approved:
                return 'Approved';
            case SellerStatuses.Rejected:
                return 'Rejected';
            default:
                return 'Pending';
        }
    }

    private mapPublic(raw: any): PublicSellerDto {
        return {
            id: raw.id ?? raw.Id,
            fullName: raw.fullName ?? raw.FullName ?? '',
            city: raw.city ?? raw.City,
            phoneNumber: raw.phoneNumber ?? raw.PhoneNumber,
            companyName: raw.companyName ?? raw.CompanyName,
            commissionPerSalePkr:
                raw.commissionPerSalePkr ?? raw.CommissionPerSalePkr ?? 3000,
        };
    }

    private mapDto(raw: any): SellerDto {
        if (!raw) {
            return { fullName: '', phoneNumber: '', status: SellerStatuses.Pending };
        }
        const status = Number(raw.status ?? raw.Status ?? SellerStatuses.Pending) as SellerStatus;
        return {
            id: raw.id ?? raw.Id,
            fullName: raw.fullName ?? raw.FullName ?? '',
            email: raw.email ?? raw.Email,
            phoneNumber: raw.phoneNumber ?? raw.PhoneNumber ?? '',
            city: raw.city ?? raw.City,
            cnic: raw.cnic ?? raw.CNIC ?? raw.Cnic,
            companyName: raw.companyName ?? raw.CompanyName,
            experienceNotes: raw.experienceNotes ?? raw.ExperienceNotes,
            message: raw.message ?? raw.Message,
            status,
            statusName: raw.statusName ?? raw.StatusName ?? this.statusLabel(status),
            isPubliclyListed: raw.isPubliclyListed ?? raw.IsPubliclyListed ?? false,
            commissionPerSalePkr:
                raw.commissionPerSalePkr ?? raw.CommissionPerSalePkr ?? 3000,
            creationTime: raw.creationTime ?? raw.CreationTime,
            reviewedTime: raw.reviewedTime ?? raw.ReviewedTime,
            reviewNotes: raw.reviewNotes ?? raw.ReviewNotes,
            rejectionReason: raw.rejectionReason ?? raw.RejectionReason,
        };
    }

    private unwrap(res: any, fallback: string): any {
        if (!res) {
            throw new Error(fallback);
        }
        if (res.success === false || res.error) {
            throw new Error(res.error?.message || res.error?.details || fallback);
        }
        return res.result ?? res;
    }
}
