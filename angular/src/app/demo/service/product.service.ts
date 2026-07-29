import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
    CreateProductDto,
    PagedProductResultRequestDto,
    PagedResultDto,
    Product,
    ProductDto,
} from 'src/app/demo/api/product';
import { environment } from '../../../environments/environment';

@Injectable({
    providedIn: 'root',
})
export class ProductService {
    private readonly apiUrl = `${environment.apiUrl}/api/services/app/Product`;

    constructor(private http: HttpClient) {}

    // Demo product helpers used by Sales dashboard widgets
    getProducts() {
        return this.http
            .get<any>('assets/demo/data/products.json')
            .toPromise()
            .then((res) => res.data as Product[])
            .then((data) => data);
    }

    getProductsMixed() {
        return this.http
            .get<any>('assets/demo/data/products-mixed.json')
            .toPromise()
            .then((res) => res.data as Product[])
            .then((data) => data);
    }

    // POS / ABP Product API
    async getAll(
        input?: PagedProductResultRequestDto
    ): Promise<PagedResultDto<ProductDto>> {
        const params: any = {};
        if (input?.keyword) {
            params.Keyword = input.keyword;
        }
        if (input?.categoryId !== undefined) {
            params.CategoryId = input.categoryId;
        }
        if (input?.brandId !== undefined) {
            params.BrandId = input.brandId;
        }
        if (input?.unitId !== undefined) {
            params.UnitId = input.unitId;
        }
        if (input?.skipCount !== undefined) {
            params.SkipCount = input.skipCount;
        }
        if (input?.maxResultCount !== undefined) {
            params.MaxResultCount = input.maxResultCount;
        }

        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetAll`, { params })
        );
        const result = this.unwrap(res, 'Failed to load products');
        const items = result.items || result.Items || [];
        const totalCount = result.totalCount ?? result.TotalCount ?? items.length;

        return {
            items: (Array.isArray(items) ? items : []).map((item: any) =>
                this.mapProduct(item)
            ),
            totalCount,
        };
    }

    async get(id: number): Promise<ProductDto> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/Get`, { params: { Id: id } })
        );
        return this.mapProduct(this.unwrap(res, 'Failed to load product'));
    }

    async create(input: CreateProductDto): Promise<ProductDto> {
        try {
            const res: any = await firstValueFrom(
                this.http.post<any>(`${this.apiUrl}/Create`, input)
            );
            return this.mapProduct(this.unwrap(res, 'Failed to create product'));
        } catch (error) {
            throw new Error(this.extractErrorMessage(error, 'Failed to create product'));
        }
    }

    async update(input: ProductDto): Promise<ProductDto> {
        try {
            const res: any = await firstValueFrom(
                this.http.put<any>(`${this.apiUrl}/Update`, {
                    id: input.id,
                    name: input.name,
                    description: input.description,
                    barcode: input.barcode,
                    price: input.price,
                    wholesalePrice: input.wholesalePrice,
                    costPrice: input.costPrice,
                    alertQuantityLimit: input.alertQuantityLimit,
                    categoryId: input.categoryId,
                    brandId: input.brandId,
                    unitId: input.unitId,
                    imagePath: input.imagePath,
                    imageBase64: input.imageBase64,
                })
            );
            return this.mapProduct(this.unwrap(res, 'Failed to update product'));
        } catch (error) {
            throw new Error(this.extractErrorMessage(error, 'Failed to update product'));
        }
    }

    async delete(id: number): Promise<void> {
        const res: any = await firstValueFrom(
            this.http.delete<any>(`${this.apiUrl}/Delete`, {
                params: { Id: id },
            })
        );
        if (res == null) {
            return;
        }
        this.unwrap(res, 'Failed to delete product');
    }

    getImageUrl(imagePath?: string): string {
        if (!imagePath) {
            return '';
        }
        if (imagePath.startsWith('http://') || imagePath.startsWith('https://') || imagePath.startsWith('data:')) {
            return imagePath;
        }
        return `${environment.apiUrl}${imagePath.startsWith('/') ? '' : '/'}${imagePath}`;
    }

    /** First two letters of the product name for image placeholders. */
    getProductInitials(name?: string | null): string {
        const cleaned = (name || '').trim().replace(/\s+/g, '');
        if (!cleaned) {
            return '?';
        }
        return cleaned.substring(0, 2).toUpperCase();
    }

    private unwrap(res: any, fallbackMessage: string): any {
        if (!res) {
            throw new Error('No response from server');
        }
        if (res.success === false || res.error) {
            throw new Error(
                res.error?.message || res.error?.details || fallbackMessage
            );
        }
        return res.result ?? res;
    }

    private extractErrorMessage(error: any, fallbackMessage: string): string {
        const abpError = error?.error;
        return (
            abpError?.error?.message ||
            abpError?.message ||
            abpError?.error?.details ||
            error?.message ||
            fallbackMessage
        );
    }

    private mapProduct(item: any): ProductDto {
        return {
            id: item.id ?? item.Id,
            name: item.name ?? item.Name,
            description: item.description ?? item.Description,
            barcode: item.barcode ?? item.Barcode,
            price: item.price ?? item.Price ?? 0,
            wholesalePrice: item.wholesalePrice ?? item.WholesalePrice ?? 0,
            costPrice: item.costPrice ?? item.CostPrice ?? 0,
            profitPerUnit: item.profitPerUnit ?? item.ProfitPerUnit ?? 0,
            profitMarginPercent:
                item.profitMarginPercent ?? item.ProfitMarginPercent,
            stockProfit: item.stockProfit ?? item.StockProfit ?? 0,
            stockQuantity: item.stockQuantity ?? item.StockQuantity ?? 0,
            alertQuantityLimit:
                item.alertQuantityLimit ?? item.AlertQuantityLimit ?? 0,
            categoryId: item.categoryId ?? item.CategoryId,
            categoryName: item.categoryName ?? item.CategoryName,
            brandId: item.brandId ?? item.BrandId,
            brandName: item.brandName ?? item.BrandName,
            unitId: item.unitId ?? item.UnitId,
            unitName: item.unitName ?? item.UnitName,
            imagePath: item.imagePath ?? item.ImagePath,
            imageBase64: item.imageBase64 ?? item.ImageBase64,
        };
    }
}
