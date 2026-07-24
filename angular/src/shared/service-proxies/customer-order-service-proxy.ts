import { mergeMap as _observableMergeMap, catchError as _observableCatch } from 'rxjs/operators';
import { Observable, throwError as _observableThrow, of as _observableOf } from 'rxjs';
import { Injectable, Inject, Optional } from '@angular/core';
import { HttpClient, HttpHeaders, HttpResponse, HttpResponseBase } from '@angular/common/http';
import { API_BASE_URL, SaleDto } from './service-proxies';

export enum CustomerOrderStatus {
    Pending = 0,
    Approved = 1,
    Rejected = 2
}

export class CreateCustomerOrderLineDto implements ICreateCustomerOrderLineDto {
    productId: number;
    quantity: number;
    unitPrice: number;

    constructor(data?: ICreateCustomerOrderLineDto) {
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
            this.quantity = _data['quantity'];
            this.unitPrice = _data['unitPrice'];
        }
    }

    static fromJS(data: any): CreateCustomerOrderLineDto {
        data = typeof data === 'object' ? data : {};
        let result = new CreateCustomerOrderLineDto();
        result.init(data);
        return result;
    }

    toJSON(data?: any) {
        data = typeof data === 'object' ? data : {};
        data['productId'] = this.productId;
        data['quantity'] = this.quantity;
        data['unitPrice'] = this.unitPrice;
        return data;
    }
}

export interface ICreateCustomerOrderLineDto {
    productId: number;
    quantity: number;
    unitPrice: number;
}

export class CreateCustomerOrderDto implements ICreateCustomerOrderDto {
    customerId: number;
    orderDate: Date;
    notes: string | undefined;
    lines: CreateCustomerOrderLineDto[];

    constructor(data?: ICreateCustomerOrderDto) {
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
            this.customerId = _data['customerId'];
            this.orderDate = _data['orderDate'] ? new Date(_data['orderDate'].toString()) : (undefined as any);
            this.notes = _data['notes'];
            if (Array.isArray(_data['lines'])) {
                this.lines = [] as any;
                for (let item of _data['lines']) {
                    this.lines.push(CreateCustomerOrderLineDto.fromJS(item));
                }
            }
        }
    }

    static fromJS(data: any): CreateCustomerOrderDto {
        data = typeof data === 'object' ? data : {};
        let result = new CreateCustomerOrderDto();
        result.init(data);
        return result;
    }

    toJSON(data?: any) {
        data = typeof data === 'object' ? data : {};
        data['customerId'] = this.customerId;
        data['orderDate'] = this.orderDate
            ? this.orderDate instanceof Date
                ? this.orderDate.toISOString()
                : this.orderDate
            : undefined;
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

export interface ICreateCustomerOrderDto {
    customerId: number;
    orderDate: Date;
    notes: string | undefined;
    lines: CreateCustomerOrderLineDto[];
}

export class CustomerOrderLineDto implements ICustomerOrderLineDto {
    id: number;
    orderId: number;
    productId: number;
    productName: string | undefined;
    quantity: number;
    unitPrice: number;
    lineTotal: number;

    constructor(data?: ICustomerOrderLineDto) {
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
            this.orderId = _data['orderId'];
            this.productId = _data['productId'];
            this.productName = _data['productName'];
            this.quantity = _data['quantity'];
            this.unitPrice = _data['unitPrice'];
            this.lineTotal = _data['lineTotal'];
        }
    }

    static fromJS(data: any): CustomerOrderLineDto {
        data = typeof data === 'object' ? data : {};
        let result = new CustomerOrderLineDto();
        result.init(data);
        return result;
    }
}

export interface ICustomerOrderLineDto {
    id: number;
    orderId: number;
    productId: number;
    productName: string | undefined;
    quantity: number;
    unitPrice: number;
    lineTotal: number;
}

export class CustomerOrderDto implements ICustomerOrderDto {
    id: number;
    customerId: number;
    customerName: string | undefined;
    orderDate: Date;
    orderNo: string | undefined;
    status: CustomerOrderStatus;
    statusName: string | undefined;
    totalAmount: number;
    notes: string | undefined;
    saleId: number | undefined;
    saleInvoiceNo: string | undefined;
    lines: CustomerOrderLineDto[] | undefined;

    constructor(data?: ICustomerOrderDto) {
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
            this.customerId = _data['customerId'];
            this.customerName = _data['customerName'];
            this.orderDate = _data['orderDate'] ? new Date(_data['orderDate'].toString()) : (undefined as any);
            this.orderNo = _data['orderNo'];
            this.status = _data['status'];
            this.statusName = _data['statusName'];
            this.totalAmount = _data['totalAmount'];
            this.notes = _data['notes'];
            this.saleId = _data['saleId'];
            this.saleInvoiceNo = _data['saleInvoiceNo'];
            if (Array.isArray(_data['lines'])) {
                this.lines = [] as any;
                for (let item of _data['lines']) {
                    this.lines.push(CustomerOrderLineDto.fromJS(item));
                }
            }
        }
    }

    static fromJS(data: any): CustomerOrderDto {
        data = typeof data === 'object' ? data : {};
        let result = new CustomerOrderDto();
        result.init(data);
        return result;
    }
}

export interface ICustomerOrderDto {
    id: number;
    customerId: number;
    customerName: string | undefined;
    orderDate: Date;
    orderNo: string | undefined;
    status: CustomerOrderStatus;
    statusName: string | undefined;
    totalAmount: number;
    notes: string | undefined;
    saleId: number | undefined;
    saleInvoiceNo: string | undefined;
    lines: CustomerOrderLineDto[] | undefined;
}

export class CustomerOrderDtoPagedResultDto implements ICustomerOrderDtoPagedResultDto {
    items: CustomerOrderDto[] | undefined;
    totalCount: number;

    constructor(data?: ICustomerOrderDtoPagedResultDto) {
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
                    this.items.push(CustomerOrderDto.fromJS(item));
                }
            }
            this.totalCount = _data['totalCount'];
        }
    }

    static fromJS(data: any): CustomerOrderDtoPagedResultDto {
        data = typeof data === 'object' ? data : {};
        let result = new CustomerOrderDtoPagedResultDto();
        result.init(data);
        return result;
    }
}

export interface ICustomerOrderDtoPagedResultDto {
    items: CustomerOrderDto[] | undefined;
    totalCount: number;
}

function throwException(message: string, status: number, response: string, headers: { [key: string]: any }, result?: any): Observable<any> {
    if (result !== null && result !== undefined) {
        return _observableThrow(result);
    }
    return _observableThrow({ message, status, response, headers, result });
}

function blobToText(blob: any): Observable<string> {
    return new Observable<string>((observer: any) => {
        if (!blob) {
            observer.next('');
            observer.complete();
        } else {
            let reader = new FileReader();
            reader.onload = (event) => {
                observer.next((event.target as any).result);
                observer.complete();
            };
            reader.readAsText(blob);
        }
    });
}

@Injectable({ providedIn: 'root' })
export class CustomerOrderServiceProxy {
    private http: HttpClient;
    private baseUrl: string;
    protected jsonParseReviver: ((key: string, value: any) => any) | undefined = undefined;

    constructor(@Inject(HttpClient) http: HttpClient, @Optional() @Inject(API_BASE_URL) baseUrl?: string) {
        this.http = http;
        this.baseUrl = baseUrl ?? '';
    }

    create(body: CreateCustomerOrderDto | undefined): Observable<CustomerOrderDto> {
        let url_ = this.baseUrl + '/api/services/app/CustomerOrder/Create';
        url_ = url_.replace(/[?&]$/, '');
        const content_ = JSON.stringify(body);
        let options_: any = {
            body: content_,
            observe: 'response',
            responseType: 'blob',
            headers: new HttpHeaders({ 'Content-Type': 'application/json', Accept: 'text/plain' }),
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
                            return _observableThrow(e) as any as Observable<CustomerOrderDto>;
                        }
                    } else {
                        return _observableThrow(response_) as any as Observable<CustomerOrderDto>;
                    }
                })
            );
    }

    protected processCreate(response: HttpResponseBase): Observable<CustomerOrderDto> {
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
                    return _observableOf(CustomerOrderDto.fromJS(resultData200));
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
        let url_ = this.baseUrl + '/api/services/app/CustomerOrder/Delete?';
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

    get(id: number | undefined): Observable<CustomerOrderDto> {
        let url_ = this.baseUrl + '/api/services/app/CustomerOrder/Get?';
        if (id === null) throw new Error("The parameter 'id' cannot be null.");
        else if (id !== undefined) url_ += 'Id=' + encodeURIComponent('' + id) + '&';
        url_ = url_.replace(/[?&]$/, '');
        let options_: any = {
            observe: 'response',
            responseType: 'blob',
            headers: new HttpHeaders({ Accept: 'text/plain' }),
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
                            return _observableThrow(e) as any as Observable<CustomerOrderDto>;
                        }
                    } else {
                        return _observableThrow(response_) as any as Observable<CustomerOrderDto>;
                    }
                })
            );
    }

    protected processGet(response: HttpResponseBase): Observable<CustomerOrderDto> {
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
                    return _observableOf(CustomerOrderDto.fromJS(resultData200));
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
        status: CustomerOrderStatus | undefined,
        skipCount: number | undefined,
        maxResultCount: number | undefined
    ): Observable<CustomerOrderDtoPagedResultDto> {
        let url_ = this.baseUrl + '/api/services/app/CustomerOrder/GetAll?';
        if (keyword === null) throw new Error("The parameter 'keyword' cannot be null.");
        else if (keyword !== undefined) url_ += 'Keyword=' + encodeURIComponent('' + keyword) + '&';
        if (status === null) throw new Error("The parameter 'status' cannot be null.");
        else if (status !== undefined) url_ += 'Status=' + encodeURIComponent('' + status) + '&';
        if (skipCount === null) throw new Error("The parameter 'skipCount' cannot be null.");
        else if (skipCount !== undefined) url_ += 'SkipCount=' + encodeURIComponent('' + skipCount) + '&';
        if (maxResultCount === null) throw new Error("The parameter 'maxResultCount' cannot be null.");
        else if (maxResultCount !== undefined) url_ += 'MaxResultCount=' + encodeURIComponent('' + maxResultCount) + '&';
        url_ = url_.replace(/[?&]$/, '');
        let options_: any = {
            observe: 'response',
            responseType: 'blob',
            headers: new HttpHeaders({ Accept: 'text/plain' }),
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
                            return _observableThrow(e) as any as Observable<CustomerOrderDtoPagedResultDto>;
                        }
                    } else {
                        return _observableThrow(response_) as any as Observable<CustomerOrderDtoPagedResultDto>;
                    }
                })
            );
    }

    protected processGetAll(response: HttpResponseBase): Observable<CustomerOrderDtoPagedResultDto> {
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
                    return _observableOf(CustomerOrderDtoPagedResultDto.fromJS(resultData200));
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

    approve(id: number | undefined): Observable<SaleDto> {
        let url_ = this.baseUrl + '/api/services/app/CustomerOrder/Approve';
        url_ = url_.replace(/[?&]$/, '');
        const content_ = JSON.stringify({ id: id });
        let options_: any = {
            body: content_,
            observe: 'response',
            responseType: 'blob',
            headers: new HttpHeaders({ 'Content-Type': 'application/json', Accept: 'text/plain' }),
        };
        return this.http
            .request('post', url_, options_)
            .pipe(_observableMergeMap((response_: any) => this.processApprove(response_)))
            .pipe(
                _observableCatch((response_: any) => {
                    if (response_ instanceof HttpResponseBase) {
                        try {
                            return this.processApprove(response_ as any);
                        } catch (e) {
                            return _observableThrow(e) as any as Observable<SaleDto>;
                        }
                    } else {
                        return _observableThrow(response_) as any as Observable<SaleDto>;
                    }
                })
            );
    }

    protected processApprove(response: HttpResponseBase): Observable<SaleDto> {
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
                    return _observableOf(SaleDto.fromJS(resultData200));
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

    reject(id: number | undefined): Observable<void> {
        let url_ = this.baseUrl + '/api/services/app/CustomerOrder/Reject';
        url_ = url_.replace(/[?&]$/, '');
        const content_ = JSON.stringify({ id: id });
        let options_: any = {
            body: content_,
            observe: 'response',
            responseType: 'blob',
            headers: new HttpHeaders({ 'Content-Type': 'application/json', Accept: 'text/plain' }),
        };
        return this.http
            .request('post', url_, options_)
            .pipe(_observableMergeMap((response_: any) => this.processReject(response_)))
            .pipe(
                _observableCatch((response_: any) => {
                    if (response_ instanceof HttpResponseBase) {
                        try {
                            return this.processReject(response_ as any);
                        } catch (e) {
                            return _observableThrow(e) as any as Observable<void>;
                        }
                    } else {
                        return _observableThrow(response_) as any as Observable<void>;
                    }
                })
            );
    }

    protected processReject(response: HttpResponseBase): Observable<void> {
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
