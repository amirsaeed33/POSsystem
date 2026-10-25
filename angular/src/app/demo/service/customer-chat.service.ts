import { HttpClient } from '@angular/common/http';
import { Injectable, OnDestroy } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { BehaviorSubject, Observable, Subject, firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
    ChatMessageSenderType,
    CustomerChatMessageDto,
    CustomerOrderConversationDto,
    SendCustomerChatMessageInput
} from '../api/customer-chat';

@Injectable({ providedIn: 'root' })
export class CustomerChatService implements OnDestroy {
    private readonly apiUrl = `${environment.apiUrl}/api/services/app/CustomerChat`;
    private readonly hubUrl = `${environment.apiUrl}/signalr-order-chat`;

    private hubConnection?: HubConnection;
    private connectionPromise?: Promise<void>;

    private messageReceivedSubject = new Subject<CustomerChatMessageDto>();
    public messageReceived$: Observable<CustomerChatMessageDto> = this.messageReceivedSubject.asObservable();

    private notificationReceivedSubject = new Subject<CustomerChatMessageDto>();
    public notificationReceived$: Observable<CustomerChatMessageDto> = this.notificationReceivedSubject.asObservable();

    private newOrderReceivedSubject = new Subject<any>();
    public newOrderReceived$: Observable<any> = this.newOrderReceivedSubject.asObservable();

    private orderStatusChangedSubject = new Subject<any>();
    public orderStatusChanged$: Observable<any> = this.orderStatusChangedSubject.asObservable();

    private isConnectedSubject = new BehaviorSubject<boolean>(false);
    public isConnected$: Observable<boolean> = this.isConnectedSubject.asObservable();

    private currentJoinedOrderNo?: string;
    private currentJoinedBranchId?: number;

    constructor(private http: HttpClient) {}

    public getSessionId(): string {
        let sid = localStorage.getItem('pos_guest_chat_session');
        if (!sid) {
            sid = 'guest_' + Math.random().toString(36).substring(2, 10) + '_' + Date.now().toString(36);
            localStorage.setItem('pos_guest_chat_session', sid);
        }
        return sid;
    }

    public async startConnection(): Promise<void> {
        if (this.hubConnection && this.hubConnection.state === HubConnectionState.Connected) {
            this.isConnectedSubject.next(true);
            return;
        }

        if (this.connectionPromise) {
            return this.connectionPromise;
        }

        this.connectionPromise = (async () => {
            try {
                this.hubConnection = new HubConnectionBuilder()
                    .withUrl(this.hubUrl)
                    .withAutomaticReconnect([0, 2000, 5000, 10000, 20000])
                    .configureLogging(LogLevel.Warning)
                    .build();

                this.hubConnection.on('ReceiveMessage', (message: CustomerChatMessageDto) => {
                    this.messageReceivedSubject.next(message);
                });

                this.hubConnection.on('OrderMessageNotification', (message: CustomerChatMessageDto) => {
                    this.notificationReceivedSubject.next(message);
                });

                this.hubConnection.on('NewOrderReceived', (order: any) => {
                    this.newOrderReceivedSubject.next(order);
                });

                this.hubConnection.on('OrderStatusChanged', (order: any) => {
                    this.orderStatusChangedSubject.next(order);
                });

                this.hubConnection.onreconnected(() => {
                    this.isConnectedSubject.next(true);
                    if (this.currentJoinedOrderNo) {
                        this.joinOrderChat(this.currentJoinedOrderNo);
                    }
                    if (this.currentJoinedBranchId) {
                        this.joinBranchChat(this.currentJoinedBranchId);
                    }
                });

                this.hubConnection.onclose(() => {
                    this.isConnectedSubject.next(false);
                });

                await this.hubConnection.start();
                this.isConnectedSubject.next(true);

                if (this.currentJoinedOrderNo) {
                    await this.hubConnection.invoke('JoinOrderChat', this.currentJoinedOrderNo);
                }
                if (this.currentJoinedBranchId) {
                    await this.hubConnection.invoke('JoinBranchChat', this.currentJoinedBranchId);
                }
            } catch (err) {
                console.warn('SignalR chat connection failed, continuing with HTTP REST fallback:', err);
                this.isConnectedSubject.next(false);
            } finally {
                this.connectionPromise = undefined;
            }
        })();

        return this.connectionPromise;
    }

    public async joinOrderChat(orderNo: string): Promise<void> {
        if (!orderNo) return;
        this.currentJoinedOrderNo = orderNo.trim();
        await this.startConnection();
        if (this.hubConnection && this.hubConnection.state === HubConnectionState.Connected) {
            try {
                await this.hubConnection.invoke('JoinOrderChat', this.currentJoinedOrderNo);
            } catch (err) {
                console.warn('Failed to join order chat SignalR group:', err);
            }
        }
    }

    public async leaveOrderChat(orderNo: string): Promise<void> {
        if (!orderNo) return;
        if (this.currentJoinedOrderNo === orderNo.trim()) {
            this.currentJoinedOrderNo = undefined;
        }
        if (this.hubConnection && this.hubConnection.state === HubConnectionState.Connected) {
            try {
                await this.hubConnection.invoke('LeaveOrderChat', orderNo.trim());
            } catch (err) {
                console.warn('Failed to leave order chat SignalR group:', err);
            }
        }
    }

    public async joinBranchChat(branchId: number): Promise<void> {
        if (!branchId || branchId <= 0) return;
        this.currentJoinedBranchId = branchId;
        await this.startConnection();
        if (this.hubConnection && this.hubConnection.state === HubConnectionState.Connected) {
            try {
                await this.hubConnection.invoke('JoinBranchChat', branchId);
            } catch (err) {
                console.warn('Failed to join branch chat group:', err);
            }
        }
    }

    public async joinSessionChat(sessionId: string): Promise<void> {
        if (!sessionId) return;
        await this.startConnection();
        if (this.hubConnection && this.hubConnection.state === HubConnectionState.Connected) {
            try {
                await this.hubConnection.invoke('JoinSessionChat', sessionId.trim());
            } catch (err) {
                console.warn('Failed to join session chat group:', err);
            }
        }
    }

    public async getMessagesByOrderNo(orderNo: string): Promise<CustomerChatMessageDto[]> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetMessagesByOrderNo`, {
                params: { orderNo }
            })
        );
        const result = res?.result !== undefined ? res.result : res;
        return Array.isArray(result) ? result : [];
    }

    public async getMessagesBySession(sessionId: string): Promise<CustomerChatMessageDto[]> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetMessagesBySession`, {
                params: { sessionId }
            })
        );
        const result = res?.result !== undefined ? res.result : res;
        return Array.isArray(result) ? result : [];
    }

    public async sendMessage(input: SendCustomerChatMessageInput): Promise<CustomerChatMessageDto> {
        await this.startConnection();

        if (this.hubConnection && this.hubConnection.state === HubConnectionState.Connected) {
            try {
                const message = (await this.hubConnection.invoke('SendMessage', input)) as CustomerChatMessageDto;
                if (message) {
                    return message;
                }
            } catch (err) {
                console.warn('SignalR SendMessage failed, falling back to HTTP POST:', err);
            }
        }

        // HTTP REST fallback
        const res: any = await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/SendMessage`, input)
        );
        const result = res?.result !== undefined ? res.result : res;
        return result;
    }

    public async markOrderMessagesAsRead(orderNo: string, isStore: boolean = true): Promise<void> {
        await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/MarkOrderMessagesAsRead`, null, {
                params: { orderNo, isStore }
            })
        );
    }

    public async markSessionMessagesAsRead(sessionId: string, branchId?: number, isStore: boolean = true): Promise<void> {
        const params: any = { sessionId, isCustomer: !isStore };
        if (branchId) params.branchId = branchId;
        await firstValueFrom(
            this.http.post<any>(`${this.apiUrl}/MarkSessionMessagesAsRead`, null, { params })
        );
    }

    public async getBranchConversations(branchId: number): Promise<CustomerOrderConversationDto[]> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetBranchConversations`, {
                params: { branchId }
            })
        );
        const result = res?.result !== undefined ? res.result : res;
        return Array.isArray(result) ? result : [];
    }

    public async getUnreadCountForOrder(orderNo: string, isStore: boolean = true): Promise<number> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetUnreadCountForOrder`, {
                params: { orderNo, isStore }
            })
        );
        const result = res?.result !== undefined ? res.result : res;
        return typeof result === 'number' ? result : 0;
    }

    public async getBranchUnreadCount(branchId: number): Promise<number> {
        const res: any = await firstValueFrom(
            this.http.get<any>(`${this.apiUrl}/GetBranchUnreadCount`, {
                params: { branchId }
            })
        );
        const result = res?.result !== undefined ? res.result : res;
        return typeof result === 'number' ? result : 0;
    }

    ngOnDestroy(): void {
        if (this.hubConnection) {
            this.hubConnection.stop().catch(() => {});
        }
    }
}
