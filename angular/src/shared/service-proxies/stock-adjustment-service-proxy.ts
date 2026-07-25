import { mergeMap as _observableMergeMap, catchError as _observableCatch } from 'rxjs/operators';
import { Observable, throwError as _observableThrow, of as _observableOf } from 'rxjs';
import { Injectable, Inject, Optional } from '@angular/core';
import { HttpClient, HttpHeaders, HttpResponse, HttpResponseBase } from '@angular/common/http';
import { API_BASE_URL } from './service-proxies';

function blobToText(blob: any): Observable<string> {
    return new Observable<string>((observer: any) => {
        if (!blob) {
            observer.next('');
            observer.complete();
        } else {
            const reader = new FileReader();
            reader.onload = (event) => {
                observer.next((event.target as any).result);
                observer.complete();
            };
            reader.readAsText(blob);
        }
    });
}

function throwException(message: string, status: number, response: string, headers: { [key: string]: any }, result?: any): Observable<any> {
    if (result !== null && result !== undefined) {
        return _observableThrow(result);
    } else {
        return _observableThrow(new Error(message + (response ? ' ' + response : '') + ' Status: ' + status));
    }
}

export enum StockAdjustmentReasons {
    Opening = 0,
    Damage = 1,
    Loss = 2,
    Recount = 3,
    Other = 4
}

export class CreateStockAdjustmentLineDto implements ICreateStockAdjustmentLineDto {
    productId: number;
    quantityChange: number;

    constructor(data?: ICreateStockAdjustmentLineDto) {
        if (data) {
            for (var property in data) {
                if (data.hasOwnProperty(property)) {
                    (<any>this)[property] = (<any>data)[property];
                }
            }
        }
    }

    init(_data?: any) {
        if (_data) {
            this.productId = _data['productId'];
            this.quantityChange = _data['quantityChange'];
        }
    }

    static fromJS(data: any): CreateStockAdjustmentLineDto {
        data = typeof data === 'object' ? data : {};
        let result = new CreateStockAdjustmentLineDto();
        result.init(data);
        return result;
    }

    toJSON(data?: any) {
        data = typeof data === 'object' ? data : {};
        data['productId'] = this.productId;
        data['quantityChange'] = this.quantityChange;
        return data;
    }
}

export interface ICreateStockAdjustmentLineDto {
    productId: number;
    quantityChange: number;
}

export class CreateStockAdjustmentDto implements ICreateStockAdjustmentDto {
    adjustmentDate: Date;
    reason: number;
    notes: string | undefined;
    lines: CreateStockAdjustmentLineDto[];

    constructor(data?: ICreateStockAdjustmentDto) {
        if (data) {
            for (var property in data) {
                if (data.hasOwnProperty(property)) {
                    (<any>this)[property] = (<any>data)[property];
                }
            }
        }
        if (this.reason === undefined) {
            this.reason = StockAdjustmentReasons.Other;
        }
    }

    init(_data?: any) {
        if (_data) {
            this.adjustmentDate = _data['adjustmentDate'] ? new Date(_data['adjustmentDate'].toString()) : (undefined as any);
            this.reason = _data['reason'] !== undefined && _data['reason'] !== null ? _data['reason'] : StockAdjustmentReasons.Other;
            this.notes = _data['notes'];
            if (Array.isArray(_data['lines'])) {
                this.lines = [] as any;
                for (let item of _data['lines']) {
                    this.lines.push(CreateStockAdjustmentLineDto.fromJS(item));
                }
            }
        }
    }

    static fromJS(data: any): CreateStockAdjustmentDto {
        data = typeof data === 'object' ? data : {};
        let result = new CreateStockAdjustmentDto();
        result.init(data);
        return result;
    }

    toJSON(data?: any) {
        data = typeof data === 'object' ? data : {};
        data['adjustmentDate'] = this.adjustmentDate
            ? this.adjustmentDate instanceof Date
                ? this.adjustmentDate.toISOString()
                : this.adjustmentDate
            : undefined;
        data['reason'] = this.reason;
        data['notes'] = this.notes;
        if (Array.isArray(this.lines)) {
            data['lines'] = [];
            for (let item of this.lines) {
                data['lines'].push(item.toJSON());
            }
        }
        return data;
    }
}

export interface ICreateStockAdjustmentDto {
    adjustmentDate: Date;
    reason: number;
    notes: string | undefined;
    lines: CreateStockAdjustmentLineDto[];
}

export class StockAdjustmentLineDto implements IStockAdjustmentLineDto {
    id: number;
    stockAdjustmentId: number;
    productId: number;
    productName: string | undefined;
    quantityChange: number;

    constructor(data?: IStockAdjustmentLineDto) {
        if (data) {
            for (var property in data) {
                if (data.hasOwnProperty(property)) {
                    (<any>this)[property] = (<any>data)[property];
                }
            }
        }
    }

    init(_data?: any) {
        if (_data) {
            this.id = _data['id'];
            this.stockAdjustmentId = _data['stockAdjustmentId'];
            this.productId = _data['productId'];
            this.productName = _data['productName'];
            this.quantityChange = _data['quantityChange'];
        }
    }

    static fromJS(data: any): StockAdjustmentLineDto {
        data = typeof data === 'object' ? data : {};
        let result = new StockAdjustmentLineDto();
        result.init(data);
        return result;
    }

    toJSON(data?: any) {
        data = typeof data === 'object' ? data : {};
        data['id'] = this.id;
        data['stockAdjustmentId'] = this.stockAdjustmentId;
        data['productId'] = this.productId;
        data['productName'] = this.productName;
        data['quantityChange'] = this.quantityChange;
        return data;
    }
}

export interface IStockAdjustmentLineDto {
    id: number;
    stockAdjustmentId: number;
    productId: number;
    productName: string | undefined;
    quantityChange: number;
}

export class StockAdjustmentDto implements IStockAdjustmentDto {
    id: number;
    adjustmentDate: Date;
    reason: number;
    referenceNo: string | undefined;
    notes: string | undefined;
    lines: StockAdjustmentLineDto[] | undefined;

    constructor(data?: IStockAdjustmentDto) {
        if (data) {
            for (var property in data) {
                if (data.hasOwnProperty(property)) {
                    (<any>this)[property] = (<any>data)[property];
                }
            }
        }
    }

    init(_data?: any) {
        if (_data) {
            this.id = _data['id'];
            this.adjustmentDate = _data['adjustmentDate'] ? new Date(_data['adjustmentDate'].toString()) : (undefined as any);
            this.reason = _data['reason'];
            this.referenceNo = _data['referenceNo'];
            this.notes = _data['notes'];
            if (Array.isArray(_data['lines'])) {
                this.lines = [] as any;
                for (let item of _data['lines']) {
                    this.lines.push(StockAdjustmentLineDto.fromJS(item));
                }
            }
        }
    }

    static fromJS(data: any): StockAdjustmentDto {
        data = typeof data === 'object' ? data : {};
        let result = new StockAdjustmentDto();
        result.init(data);
        return result;
    }

    toJSON(data?: any) {
        data = typeof data === 'object' ? data : {};
        data['id'] = this.id;
        data['adjustmentDate'] = this.adjustmentDate
            ? this.adjustmentDate instanceof Date
                ? this.adjustmentDate.toISOString()
                : this.adjustmentDate
            : undefined;
        data['reason'] = this.reason;
        data['referenceNo'] = this.referenceNo;
        data['notes'] = this.notes;
        if (Array.isArray(this.lines)) {
            data['lines'] = [];
            for (let item of this.lines) {
                data['lines'].push(item.toJSON());
            }
        }
        return data;
    }
}

export interface IStockAdjustmentDto {
    id: number;
    adjustmentDate: Date;
    reason: number;
    referenceNo: string | undefined;
    notes: string | undefined;
    lines: StockAdjustmentLineDto[] | undefined;
}

export class StockAdjustmentDtoPagedResultDto implements IStockAdjustmentDtoPagedResultDto {
    items: StockAdjustmentDto[] | undefined;
    totalCount: number;

    constructor(data?: IStockAdjustmentDtoPagedResultDto) {
        if (data) {
            for (var property in data) {
                if (data.hasOwnProperty(property)) {
                    (<any>this)[property] = (<any>data)[property];
                }
            }
        }
    }

    init(_data?: any) {
        if (_data) {
            if (Array.isArray(_data['items'])) {
                this.items = [] as any;
                for (let item of _data['items']) {
                    this.items.push(StockAdjustmentDto.fromJS(item));
                }
            }
            this.totalCount = _data['totalCount'];
        }
    }

    static fromJS(data: any): StockAdjustmentDtoPagedResultDto {
        data = typeof data === 'object' ? data : {};
        let result = new StockAdjustmentDtoPagedResultDto();
        result.init(data);
        return result;
    }

    toJSON(data?: any) {
        data = typeof data === 'object' ? data : {};
        if (Array.isArray(this.items)) {
            data['items'] = [];
            for (let item of this.items) {
                data['items'].push(item.toJSON());
            }
        }
        data['totalCount'] = this.totalCount;
        return data;
    }
}

export interface IStockAdjustmentDtoPagedResultDto {
    items: StockAdjustmentDto[] | undefined;
    totalCount: number;
}

@Injectable()
export class StockAdjustmentServiceProxy {
    private http: HttpClient;
    private baseUrl: string;
    protected jsonParseReviver: ((key: string, value: any) => any) | undefined = undefined;

    constructor(@Inject(HttpClient) http: HttpClient, @Optional() @Inject(API_BASE_URL) baseUrl?: string) {
        this.http = http;
        this.baseUrl = baseUrl ?? '';
    }

    create(body: CreateStockAdjustmentDto | undefined): Observable<StockAdjustmentDto> {
        let url_ = this.baseUrl + '/api/services/app/StockAdjustment/Create';
        url_ = url_.replace(/[?&]$/, '');
        const content_ = JSON.stringify(body);
        let options_: any = {
            body: content_,
            observe: 'response',
            responseType: 'blob',
            headers: new HttpHeaders({ 'Content-Type': 'application/json', Accept: 'text/plain' })
        };
        return this.http
            .request('post', url_, options_)
            .pipe(_observableMergeMap((response_: any) => this.processCreate(response_)))
            .pipe(
                _observableCatch((response_: any) => {
                    if (response_ instanceof HttpResponseBase) {
                        try {
                            return this.processCreate(response_ as any);
                        } catch (e) {
                            return _observableThrow(e) as any as Observable<StockAdjustmentDto>;
                        }
                    } else {
                        return _observableThrow(response_) as any as Observable<StockAdjustmentDto>;
                    }
                })
            );
    }

    protected processCreate(response: HttpResponseBase): Observable<StockAdjustmentDto> {
        const status = response.status;
        const responseBlob =
            response instanceof HttpResponse
                ? response.body
                : (response as any).error instanceof Blob
                ? (response as any).error
                : undefined;
        let _headers: any = {};
        if (response.headers) {
            for (let key of response.headers.keys()) {
                _headers[key] = response.headers.get(key);
            }
        }
        if (status === 200) {
            return blobToText(responseBlob).pipe(
                _observableMergeMap((_responseText: string) => {
                    let resultData200 = _responseText === '' ? null : JSON.parse(_responseText, this.jsonParseReviver);
                    return _observableOf(StockAdjustmentDto.fromJS(resultData200));
                })
            );
        } else if (status !== 200 && status !== 204) {
            return blobToText(responseBlob).pipe(
                _observableMergeMap((_responseText: string) =>
                    throwException('An unexpected server error occurred.', status, _responseText, _headers)
                )
            );
        }
        return _observableOf(null as any);
    }

    get(id: number | undefined): Observable<StockAdjustmentDto> {
        let url_ = this.baseUrl + '/api/services/app/StockAdjustment/Get?';
        if (id === null) throw new Error("The parameter 'id' cannot be null.");
        else if (id !== undefined) url_ += 'Id=' + encodeURIComponent('' + id) + '&';
        url_ = url_.replace(/[?&]$/, '');
        let options_: any = {
            observe: 'response',
            responseType: 'blob',
            headers: new HttpHeaders({ Accept: 'text/plain' })
        };
        return this.http
            .request('get', url_, options_)
            .pipe(_observableMergeMap((response_: any) => this.processGet(response_)))
            .pipe(
                _observableCatch((response_: any) => {
                    if (response_ instanceof HttpResponseBase) {
                        try {
                            return this.processGet(response_ as any);
                        } catch (e) {
                            return _observableThrow(e) as any as Observable<StockAdjustmentDto>;
                        }
                    } else {
                        return _observableThrow(response_) as any as Observable<StockAdjustmentDto>;
                    }
                })
            );
    }

    protected processGet(response: HttpResponseBase): Observable<StockAdjustmentDto> {
        const status = response.status;
        const responseBlob =
            response instanceof HttpResponse
                ? response.body
                : (response as any).error instanceof Blob
                ? (response as any).error
                : undefined;
        let _headers: any = {};
        if (response.headers) {
            for (let key of response.headers.keys()) {
                _headers[key] = response.headers.get(key);
            }
        }
        if (status === 200) {
            return blobToText(responseBlob).pipe(
                _observableMergeMap((_responseText: string) => {
                    let resultData200 = _responseText === '' ? null : JSON.parse(_responseText, this.jsonParseReviver);
                    return _observableOf(StockAdjustmentDto.fromJS(resultData200));
                })
            );
        } else if (status !== 200 && status !== 204) {
            return blobToText(responseBlob).pipe(
                _observableMergeMap((_responseText: string) =>
                    throwException('An unexpected server error occurred.', status, _responseText, _headers)
                )
            );
        }
        return _observableOf(null as any);
    }

    getAll(
        keyword: string | undefined,
        skipCount: number | undefined,
        maxResultCount: number | undefined
    ): Observable<StockAdjustmentDtoPagedResultDto> {
        let url_ = this.baseUrl + '/api/services/app/StockAdjustment/GetAll?';
        if (keyword === null) throw new Error("The parameter 'keyword' cannot be null.");
        else if (keyword !== undefined) url_ += 'Keyword=' + encodeURIComponent('' + keyword) + '&';
        if (skipCount === null) throw new Error("The parameter 'skipCount' cannot be null.");
        else if (skipCount !== undefined) url_ += 'SkipCount=' + encodeURIComponent('' + skipCount) + '&';
        if (maxResultCount === null) throw new Error("The parameter 'maxResultCount' cannot be null.");
        else if (maxResultCount !== undefined) url_ += 'MaxResultCount=' + encodeURIComponent('' + maxResultCount) + '&';
        url_ = url_.replace(/[?&]$/, '');
        let options_: any = {
            observe: 'response',
            responseType: 'blob',
            headers: new HttpHeaders({ Accept: 'text/plain' })
        };
        return this.http
            .request('get', url_, options_)
            .pipe(_observableMergeMap((response_: any) => this.processGetAll(response_)))
            .pipe(
                _observableCatch((response_: any) => {
                    if (response_ instanceof HttpResponseBase) {
                        try {
                            return this.processGetAll(response_ as any);
                        } catch (e) {
                            return _observableThrow(e) as any as Observable<StockAdjustmentDtoPagedResultDto>;
                        }
                    } else {
                        return _observableThrow(response_) as any as Observable<StockAdjustmentDtoPagedResultDto>;
                    }
                })
            );
    }

    protected processGetAll(response: HttpResponseBase): Observable<StockAdjustmentDtoPagedResultDto> {
        const status = response.status;
        const responseBlob =
            response instanceof HttpResponse
                ? response.body
                : (response as any).error instanceof Blob
                ? (response as any).error
                : undefined;
        let _headers: any = {};
        if (response.headers) {
            for (let key of response.headers.keys()) {
                _headers[key] = response.headers.get(key);
            }
        }
        if (status === 200) {
            return blobToText(responseBlob).pipe(
                _observableMergeMap((_responseText: string) => {
                    let resultData200 = _responseText === '' ? null : JSON.parse(_responseText, this.jsonParseReviver);
                    return _observableOf(StockAdjustmentDtoPagedResultDto.fromJS(resultData200));
                })
            );
        } else if (status !== 200 && status !== 204) {
            return blobToText(responseBlob).pipe(
                _observableMergeMap((_responseText: string) =>
                    throwException('An unexpected server error occurred.', status, _responseText, _headers)
                )
            );
        }
        return _observableOf(null as any);
    }

    delete(id: number | undefined): Observable<void> {
        let url_ = this.baseUrl + '/api/services/app/StockAdjustment/Delete?';
        if (id === null) throw new Error("The parameter 'id' cannot be null.");
        else if (id !== undefined) url_ += 'Id=' + encodeURIComponent('' + id) + '&';
        url_ = url_.replace(/[?&]$/, '');
        let options_: any = { observe: 'response', responseType: 'blob', headers: new HttpHeaders({}) };
        return this.http
            .request('delete', url_, options_)
            .pipe(_observableMergeMap((response_: any) => this.processDelete(response_)))
            .pipe(
                _observableCatch((response_: any) => {
                    if (response_ instanceof HttpResponseBase) {
                        try {
                            return this.processDelete(response_ as any);
                        } catch (e) {
                            return _observableThrow(e) as any as Observable<void>;
                        }
                    } else {
                        return _observableThrow(response_) as any as Observable<void>;
                    }
                })
            );
    }

    protected processDelete(response: HttpResponseBase): Observable<void> {
        const status = response.status;
        const responseBlob =
            response instanceof HttpResponse
                ? response.body
                : (response as any).error instanceof Blob
                ? (response as any).error
                : undefined;
        let _headers: any = {};
        if (response.headers) {
            for (let key of response.headers.keys()) {
                _headers[key] = response.headers.get(key);
            }
        }
        if (status === 200) {
            return blobToText(responseBlob).pipe(_observableMergeMap((_responseText: string) => _observableOf(null as any)));
        } else if (status !== 200 && status !== 204) {
            return blobToText(responseBlob).pipe(
                _observableMergeMap((_responseText: string) =>
                    throwException('An unexpected server error occurred.', status, _responseText, _headers)
                )
            );
        }
        return _observableOf(null as any);
    }
}
