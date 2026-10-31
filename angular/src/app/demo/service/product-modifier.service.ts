import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
    CreateModifierGroupDto,
    CreateModifierOptionDto,
    ProductModifierGroupDto,
    ProductModifierOptionDto,
} from '../api/product-modifier';
import { environment } from '../../../environments/environment';
import { TtlCache, branchCacheKey } from './ttl-cache';

@Injectable({ providedIn: 'root' })
export class ProductModifierService {
    private readonly apiUrl = `${environment.apiUrl}/api/services/app/ProductModifier`;
    private readonly byProductCache = new TtlCache<ProductModifierGroupDto[]>(
        5 * 60 * 1000
    );

    constructor(private http: HttpClient) {}

    clearCache(): void {
        this.byProductCache.clear();
    }

    async getByProduct(productId: number): Promise<ProductModifierGroupDto[]> {
        const key = branchCacheKey(this.readBranchId(), `mod:${productId}`);
        const hit = this.byProductCache.get(key);
        if (hit) {
            return hit;
        }

        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetByProduct`, {
                params: { productId },
            })
        );
        const list = this.unwrap(res, 'Failed to load modifiers');
        const mapped = (Array.isArray(list) ? list : []).map((g: any) =>
            this.mapGroup(g)
        );
        this.byProductCache.set(key, mapped);
        return mapped;
    }

    private readBranchId(): number | null {
        const n = parseInt(localStorage.getItem('SmartPos.BranchId') || '', 10);
        return Number.isFinite(n) && n > 0 ? n : null;
    }

    async createGroup(
        dto: CreateModifierGroupDto
    ): Promise<ProductModifierGroupDto> {
        this.clearCache();
        const payload = {
            productId: dto.productId,
            name: dto.name,
            minSelection: dto.minSelection ?? (dto.isRequired ? 1 : 0),
            maxSelection: dto.maxSelection ?? (dto.allowMultiple ? 10 : 1),
            isRequired: !!dto.isRequired,
            options: (dto.options || []).map((o) => ({
                name: o.name,
                price: o.price ?? o.extraPrice ?? 0,
                isDefault: !!o.isDefault,
            })),
        };
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/CreateGroup`, payload)
        );
        const group = this.unwrap(res, 'Failed to create modifier group');
        return this.mapGroup(group);
    }

    async deleteGroup(id: number): Promise<void> {
        this.clearCache();
        const res: any = await firstValueFrom(
            this.http.delete<any>(`${this.apiUrl}/DeleteGroup`, {
                params: { Id: id },
            })
        );
        this.unwrap(res, 'Failed to delete modifier group');
    }

    async addOption(
        groupId: number,
        dto: CreateModifierOptionDto
    ): Promise<ProductModifierOptionDto> {
        this.clearCache();
        const payload = {
            name: dto.name,
            price: dto.price ?? dto.extraPrice ?? 0,
            isDefault: !!dto.isDefault,
        };
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/AddOption`, payload, {
                params: { groupId },
            })
        );
        const option = this.unwrap(res, 'Failed to add option');
        return this.mapOption(option);
    }

    async deleteOption(id: number): Promise<void> {
        this.clearCache();
        const res: any = await firstValueFrom(
            this.http.delete<any>(`${this.apiUrl}/DeleteOption`, {
                params: { Id: id },
            })
        );
        this.unwrap(res, 'Failed to delete option');
    }

    private unwrap(res: any, fallback: string): any {
        if (!res) throw new Error('No response from server');
        if (res.success === false || res.error) {
            throw new Error(res.error?.message || res.error?.details || fallback);
        }
        return res.result ?? res;
    }

    private mapGroup(g: any): ProductModifierGroupDto {
        const maxSel = g.maxSelection ?? g.MaxSelection ?? 10;
        return {
            id: g.id ?? g.Id,
            productId: g.productId ?? g.ProductId,
            name: g.name ?? g.Name,
            minSelection: g.minSelection ?? g.MinSelection ?? 0,
            maxSelection: maxSel,
            isRequired: !!(g.isRequired ?? g.IsRequired),
            allowMultiple: true,
            options: (g.options || g.Options || []).map((o: any) => this.mapOption(o)),
        };
    }

    private mapOption(o: any): ProductModifierOptionDto {
        const price = o.price ?? o.Price ?? o.extraPrice ?? o.ExtraPrice ?? 0;
        return {
            id: o.id ?? o.Id,
            modifierGroupId: o.modifierGroupId ?? o.ModifierGroupId,
            name: o.name ?? o.Name,
            price: price,
            extraPrice: price,
            isDefault: !!(o.isDefault ?? o.IsDefault),
        };
    }
}
