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

@Injectable({ providedIn: 'root' })
export class ProductModifierService {
    private readonly apiUrl = `${environment.apiUrl}/api/services/app/ProductModifier`;

    constructor(private http: HttpClient) {}

    async getByProduct(productId: number): Promise<ProductModifierGroupDto[]> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetByProduct`, {
                params: { productId },
            })
        );
        const list = this.unwrap(res, 'Failed to load modifiers');
        return (Array.isArray(list) ? list : []).map((g: any) => this.mapGroup(g));
    }

    async createGroup(
        dto: CreateModifierGroupDto
    ): Promise<ProductModifierGroupDto> {
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
        const maxSel = g.maxSelection ?? g.MaxSelection ?? 1;
        const allowMulti = g.allowMultiple ?? g.AllowMultiple ?? (maxSel > 1);
        return {
            id: g.id ?? g.Id,
            productId: g.productId ?? g.ProductId,
            name: g.name ?? g.Name,
            minSelection: g.minSelection ?? g.MinSelection ?? 0,
            maxSelection: maxSel,
            isRequired: !!(g.isRequired ?? g.IsRequired),
            allowMultiple: !!allowMulti,
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
