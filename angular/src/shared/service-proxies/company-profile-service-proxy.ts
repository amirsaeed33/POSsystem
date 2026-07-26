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

export class CreateCompanyProfileDto implements ICreateCompanyProfileDto {
    name: string;
    imageBase64: string | undefined;
    invoiceAddress: string | undefined;
    invoiceContactEmail: string | undefined;
    invoiceContactPhone: string | undefined;
    taxNumber: string | undefined;
    website: string | undefined;
    invoiceFooter: string | undefined;

    constructor(data?: ICreateCompanyProfileDto) {
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
            this.name = _data['name'];
            this.imageBase64 = _data['imageBase64'];
            this.invoiceAddress = _data['invoiceAddress'];
            this.invoiceContactEmail = _data['invoiceContactEmail'];
            this.invoiceContactPhone = _data['invoiceContactPhone'];
            this.taxNumber = _data['taxNumber'];
            this.website = _data['website'];
            this.invoiceFooter = _data['invoiceFooter'];
        }
    }

    static fromJS(data: any): CreateCompanyProfileDto {
        data = typeof data === 'object' ? data : {};
        let result = new CreateCompanyProfileDto();
        result.init(data);
        return result;
    }

    toJSON(data?: any) {
        data = typeof data === 'object' ? data : {};
        data['name'] = this.name;
        data['imageBase64'] = this.imageBase64;
        data['invoiceAddress'] = this.invoiceAddress;
        data['invoiceContactEmail'] = this.invoiceContactEmail;
        data['invoiceContactPhone'] = this.invoiceContactPhone;
        data['taxNumber'] = this.taxNumber;
        data['website'] = this.website;
        data['invoiceFooter'] = this.invoiceFooter;
        return data;
    }
}

export interface ICreateCompanyProfileDto {
    name: string;
    imageBase64: string | undefined;
    invoiceAddress: string | undefined;
    invoiceContactEmail: string | undefined;
    invoiceContactPhone: string | undefined;
    taxNumber: string | undefined;
    website: string | undefined;
    invoiceFooter: string | undefined;
}

export class CompanyProfileDto implements ICompanyProfileDto {
    id: number;
    name: string;
    imagePath: string | undefined;
    imageBase64: string | undefined;
    invoiceAddress: string | undefined;
    invoiceContactEmail: string | undefined;
    invoiceContactPhone: string | undefined;
    taxNumber: string | undefined;
    website: string | undefined;
    invoiceFooter: string | undefined;

    constructor(data?: ICompanyProfileDto) {
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
            this.name = _data['name'];
            this.imagePath = _data['imagePath'];
            this.imageBase64 = _data['imageBase64'];
            this.invoiceAddress = _data['invoiceAddress'];
            this.invoiceContactEmail = _data['invoiceContactEmail'];
            this.invoiceContactPhone = _data['invoiceContactPhone'];
            this.taxNumber = _data['taxNumber'];
            this.website = _data['website'];
            this.invoiceFooter = _data['invoiceFooter'];
        }
    }

    static fromJS(data: any): CompanyProfileDto {
        data = typeof data === 'object' ? data : {};
        let result = new CompanyProfileDto();
        result.init(data);
        return result;
    }

    toJSON(data?: any) {
        data = typeof data === 'object' ? data : {};
        data['id'] = this.id;
        data['name'] = this.name;
        data['imagePath'] = this.imagePath;
        data['imageBase64'] = this.imageBase64;
        data['invoiceAddress'] = this.invoiceAddress;
        data['invoiceContactEmail'] = this.invoiceContactEmail;
        data['invoiceContactPhone'] = this.invoiceContactPhone;
        data['taxNumber'] = this.taxNumber;
        data['website'] = this.website;
        data['invoiceFooter'] = this.invoiceFooter;
        return data;
    }
}

export interface ICompanyProfileDto {
    id: number;
    name: string;
    imagePath: string | undefined;
    imageBase64: string | undefined;
    invoiceAddress: string | undefined;
    invoiceContactEmail: string | undefined;
    invoiceContactPhone: string | undefined;
    taxNumber: string | undefined;
    website: string | undefined;
    invoiceFooter: string | undefined;
}

export class CompanyProfileDtoPagedResultDto implements ICompanyProfileDtoPagedResultDto {
    items: CompanyProfileDto[] | undefined;
    totalCount: number;

    constructor(data?: ICompanyProfileDtoPagedResultDto) {
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
                    this.items!.push(CompanyProfileDto.fromJS(item));
                }
            }
            this.totalCount = _data['totalCount'];
        }
    }

    static fromJS(data: any): CompanyProfileDtoPagedResultDto {
        data = typeof data === 'object' ? data : {};
        let result = new CompanyProfileDtoPagedResultDto();
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

export interface ICompanyProfileDtoPagedResultDto {
    items: CompanyProfileDto[] | undefined;
    totalCount: number;
}

@Injectable()
export class CompanyProfileServiceProxy {
    private http: HttpClient;
    private baseUrl: string;
    protected jsonParseReviver: ((key: string, value: any) => any) | undefined = undefined;

    constructor(@Inject(HttpClient) http: HttpClient, @Optional() @Inject(API_BASE_URL) baseUrl?: string) {
        this.http = http;
        this.baseUrl = baseUrl ?? '';
    }

    getCurrent(): Observable<CompanyProfileDto> {
        let url_ = this.baseUrl + '/api/services/app/CompanyProfile/GetCurrent';
        url_ = url_.replace(/[?&]$/, '');
        let options_: any = {
            observe: 'response',
            responseType: 'blob',
            headers: new HttpHeaders({
                Accept: 'text/plain'
            })
        };
        return this.http.request('get', url_, options_).pipe(_observableMergeMap((response_: any) => {
            return this.processGetCurrent(response_);
        })).pipe(_observableCatch((response_: any) => {
            if (response_ instanceof HttpResponseBase) {
                try {
                    return this.processGetCurrent(response_ as any);
                } catch (e) {
                    return _observableThrow(e) as any as Observable<CompanyProfileDto>;
                }
            } else {
                return _observableThrow(response_) as any as Observable<CompanyProfileDto>;
            }
        }));
    }

    protected processGetCurrent(response: HttpResponseBase): Observable<CompanyProfileDto> {
        const status = response.status;
        const responseBlob =
            response instanceof HttpResponse ? response.body :
            (response as any).error instanceof Blob ? (response as any).error : undefined;
        let _headers: any = {}; if (response.headers) { for (let key of response.headers.keys()) { _headers[key] = response.headers.get(key); } }
        if (status === 200) {
            return blobToText(responseBlob).pipe(_observableMergeMap((_responseText: string) => {
                let result200: any = null;
                let resultData200 = _responseText === '' ? null : JSON.parse(_responseText, this.jsonParseReviver);
                result200 = resultData200 ? CompanyProfileDto.fromJS(resultData200) : null as any;
                return _observableOf(result200);
            }));
        } else if (status !== 200 && status !== 204) {
            return blobToText(responseBlob).pipe(_observableMergeMap((_responseText: string) => {
                return throwException('An unexpected server error occurred.', status, _responseText, _headers);
            }));
        }
        return _observableOf(null as any);
    }

    create(body: CreateCompanyProfileDto | undefined): Observable<CompanyProfileDto> {
        let url_ = this.baseUrl + '/api/services/app/CompanyProfile/Create';
        url_ = url_.replace(/[?&]$/, '');
        const content_ = JSON.stringify(body);
        let options_: any = {
            body: content_,
            observe: 'response',
            responseType: 'blob',
            headers: new HttpHeaders({
                'Content-Type': 'application/json',
                Accept: 'text/plain'
            })
        };
        return this.http.request('post', url_, options_).pipe(_observableMergeMap((response_: any) => {
            return this.processCreate(response_);
        })).pipe(_observableCatch((response_: any) => {
            if (response_ instanceof HttpResponseBase) {
                try {
                    return this.processCreate(response_ as any);
                } catch (e) {
                    return _observableThrow(e) as any as Observable<CompanyProfileDto>;
                }
            } else {
                return _observableThrow(response_) as any as Observable<CompanyProfileDto>;
            }
        }));
    }

    protected processCreate(response: HttpResponseBase): Observable<CompanyProfileDto> {
        const status = response.status;
        const responseBlob =
            response instanceof HttpResponse ? response.body :
            (response as any).error instanceof Blob ? (response as any).error : undefined;
        let _headers: any = {}; if (response.headers) { for (let key of response.headers.keys()) { _headers[key] = response.headers.get(key); } }
        if (status === 200) {
            return blobToText(responseBlob).pipe(_observableMergeMap((_responseText: string) => {
                let result200: any = null;
                let resultData200 = _responseText === '' ? null : JSON.parse(_responseText, this.jsonParseReviver);
                result200 = CompanyProfileDto.fromJS(resultData200);
                return _observableOf(result200);
            }));
        } else if (status !== 200 && status !== 204) {
            return blobToText(responseBlob).pipe(_observableMergeMap((_responseText: string) => {
                return throwException('An unexpected server error occurred.', status, _responseText, _headers);
            }));
        }
        return _observableOf(null as any);
    }

    delete(id: number | undefined): Observable<void> {
        let url_ = this.baseUrl + '/api/services/app/CompanyProfile/Delete?';
        if (id === null) {
            throw new Error("The parameter 'id' cannot be null.");
        } else if (id !== undefined) {
            url_ += 'Id=' + encodeURIComponent('' + id) + '&';
        }
        url_ = url_.replace(/[?&]$/, '');
        let options_: any = {
            observe: 'response',
            responseType: 'blob',
            headers: new HttpHeaders({})
        };
        return this.http.request('delete', url_, options_).pipe(_observableMergeMap((response_: any) => {
            return this.processDelete(response_);
        })).pipe(_observableCatch((response_: any) => {
            if (response_ instanceof HttpResponseBase) {
                try {
                    return this.processDelete(response_ as any);
                } catch (e) {
                    return _observableThrow(e) as any as Observable<void>;
                }
            } else {
                return _observableThrow(response_) as any as Observable<void>;
            }
        }));
    }

    protected processDelete(response: HttpResponseBase): Observable<void> {
        const status = response.status;
        const responseBlob =
            response instanceof HttpResponse ? response.body :
            (response as any).error instanceof Blob ? (response as any).error : undefined;
        let _headers: any = {}; if (response.headers) { for (let key of response.headers.keys()) { _headers[key] = response.headers.get(key); } }
        if (status === 200) {
            return blobToText(responseBlob).pipe(_observableMergeMap((_responseText: string) => {
                return _observableOf(null as any);
            }));
        } else if (status !== 200 && status !== 204) {
            return blobToText(responseBlob).pipe(_observableMergeMap((_responseText: string) => {
                return throwException('An unexpected server error occurred.', status, _responseText, _headers);
            }));
        }
        return _observableOf(null as any);
    }

    get(id: number | undefined): Observable<CompanyProfileDto> {
        let url_ = this.baseUrl + '/api/services/app/CompanyProfile/Get?';
        if (id === null) {
            throw new Error("The parameter 'id' cannot be null.");
        } else if (id !== undefined) {
            url_ += 'Id=' + encodeURIComponent('' + id) + '&';
        }
        url_ = url_.replace(/[?&]$/, '');
        let options_: any = {
            observe: 'response',
            responseType: 'blob',
            headers: new HttpHeaders({
                Accept: 'text/plain'
            })
        };
        return this.http.request('get', url_, options_).pipe(_observableMergeMap((response_: any) => {
            return this.processGet(response_);
        })).pipe(_observableCatch((response_: any) => {
            if (response_ instanceof HttpResponseBase) {
                try {
                    return this.processGet(response_ as any);
                } catch (e) {
                    return _observableThrow(e) as any as Observable<CompanyProfileDto>;
                }
            } else {
                return _observableThrow(response_) as any as Observable<CompanyProfileDto>;
            }
        }));
    }

    protected processGet(response: HttpResponseBase): Observable<CompanyProfileDto> {
        const status = response.status;
        const responseBlob =
            response instanceof HttpResponse ? response.body :
            (response as any).error instanceof Blob ? (response as any).error : undefined;
        let _headers: any = {}; if (response.headers) { for (let key of response.headers.keys()) { _headers[key] = response.headers.get(key); } }
        if (status === 200) {
            return blobToText(responseBlob).pipe(_observableMergeMap((_responseText: string) => {
                let result200: any = null;
                let resultData200 = _responseText === '' ? null : JSON.parse(_responseText, this.jsonParseReviver);
                result200 = CompanyProfileDto.fromJS(resultData200);
                return _observableOf(result200);
            }));
        } else if (status !== 200 && status !== 204) {
            return blobToText(responseBlob).pipe(_observableMergeMap((_responseText: string) => {
                return throwException('An unexpected server error occurred.', status, _responseText, _headers);
            }));
        }
        return _observableOf(null as any);
    }

    getAll(keyword: string | undefined, skipCount: number | undefined, maxResultCount: number | undefined): Observable<CompanyProfileDtoPagedResultDto> {
        let url_ = this.baseUrl + '/api/services/app/CompanyProfile/GetAll?';
        if (keyword === null) {
            throw new Error("The parameter 'keyword' cannot be null.");
        } else if (keyword !== undefined) {
            url_ += 'Keyword=' + encodeURIComponent('' + keyword) + '&';
        }
        if (skipCount === null) {
            throw new Error("The parameter 'skipCount' cannot be null.");
        } else if (skipCount !== undefined) {
            url_ += 'SkipCount=' + encodeURIComponent('' + skipCount) + '&';
        }
        if (maxResultCount === null) {
            throw new Error("The parameter 'maxResultCount' cannot be null.");
        } else if (maxResultCount !== undefined) {
            url_ += 'MaxResultCount=' + encodeURIComponent('' + maxResultCount) + '&';
        }
        url_ = url_.replace(/[?&]$/, '');
        let options_: any = {
            observe: 'response',
            responseType: 'blob',
            headers: new HttpHeaders({
                Accept: 'text/plain'
            })
        };
        return this.http.request('get', url_, options_).pipe(_observableMergeMap((response_: any) => {
            return this.processGetAll(response_);
        })).pipe(_observableCatch((response_: any) => {
            if (response_ instanceof HttpResponseBase) {
                try {
                    return this.processGetAll(response_ as any);
                } catch (e) {
                    return _observableThrow(e) as any as Observable<CompanyProfileDtoPagedResultDto>;
                }
            } else {
                return _observableThrow(response_) as any as Observable<CompanyProfileDtoPagedResultDto>;
            }
        }));
    }

    protected processGetAll(response: HttpResponseBase): Observable<CompanyProfileDtoPagedResultDto> {
        const status = response.status;
        const responseBlob =
            response instanceof HttpResponse ? response.body :
            (response as any).error instanceof Blob ? (response as any).error : undefined;
        let _headers: any = {}; if (response.headers) { for (let key of response.headers.keys()) { _headers[key] = response.headers.get(key); } }
        if (status === 200) {
            return blobToText(responseBlob).pipe(_observableMergeMap((_responseText: string) => {
                let result200: any = null;
                let resultData200 = _responseText === '' ? null : JSON.parse(_responseText, this.jsonParseReviver);
                result200 = CompanyProfileDtoPagedResultDto.fromJS(resultData200);
                return _observableOf(result200);
            }));
        } else if (status !== 200 && status !== 204) {
            return blobToText(responseBlob).pipe(_observableMergeMap((_responseText: string) => {
                return throwException('An unexpected server error occurred.', status, _responseText, _headers);
            }));
        }
        return _observableOf(null as any);
    }

    update(body: CompanyProfileDto | undefined): Observable<CompanyProfileDto> {
        let url_ = this.baseUrl + '/api/services/app/CompanyProfile/Update';
        url_ = url_.replace(/[?&]$/, '');
        const content_ = JSON.stringify(body);
        let options_: any = {
            body: content_,
            observe: 'response',
            responseType: 'blob',
            headers: new HttpHeaders({
                'Content-Type': 'application/json',
                Accept: 'text/plain'
            })
        };
        return this.http.request('put', url_, options_).pipe(_observableMergeMap((response_: any) => {
            return this.processUpdate(response_);
        })).pipe(_observableCatch((response_: any) => {
            if (response_ instanceof HttpResponseBase) {
                try {
                    return this.processUpdate(response_ as any);
                } catch (e) {
                    return _observableThrow(e) as any as Observable<CompanyProfileDto>;
                }
            } else {
                return _observableThrow(response_) as any as Observable<CompanyProfileDto>;
            }
        }));
    }

    protected processUpdate(response: HttpResponseBase): Observable<CompanyProfileDto> {
        const status = response.status;
        const responseBlob =
            response instanceof HttpResponse ? response.body :
            (response as any).error instanceof Blob ? (response as any).error : undefined;
        let _headers: any = {}; if (response.headers) { for (let key of response.headers.keys()) { _headers[key] = response.headers.get(key); } }
        if (status === 200) {
            return blobToText(responseBlob).pipe(_observableMergeMap((_responseText: string) => {
                let result200: any = null;
                let resultData200 = _responseText === '' ? null : JSON.parse(_responseText, this.jsonParseReviver);
                result200 = CompanyProfileDto.fromJS(resultData200);
                return _observableOf(result200);
            }));
        } else if (status !== 200 && status !== 204) {
            return blobToText(responseBlob).pipe(_observableMergeMap((_responseText: string) => {
                return throwException('An unexpected server error occurred.', status, _responseText, _headers);
            }));
        }
        return _observableOf(null as any);
    }
}
