import {
    Component,
    Input,
    Output,
    EventEmitter,
    OnInit,
    OnDestroy,
    OnChanges,
    SimpleChanges,
    ViewChild,
    ElementRef,
    AfterViewChecked
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { RippleModule } from 'primeng/ripple';
import { InputTextModule } from 'primeng/inputtext';
import { TooltipModule } from 'primeng/tooltip';
import { BadgeModule } from 'primeng/badge';
import { Subscription } from 'rxjs';
import { CustomerChatService } from 'src/app/demo/service/customer-chat.service';
import {
    ChatMessageSenderType,
    CustomerChatMessageDto,
    SendCustomerChatMessageInput
} from 'src/app/demo/api/customer-chat';

@Component({
    selector: 'app-order-chat',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        ButtonModule,
        RippleModule,
        InputTextModule,
        TooltipModule,
        BadgeModule
    ],
    templateUrl: './order-chat.component.html',
    styleUrls: ['./order-chat.component.scss']
})
export class OrderChatComponent implements OnInit, OnDestroy, OnChanges, AfterViewChecked {
    @Input() orderNo?: string;
    @Input() sessionId?: string;
    @Input() branchId?: number;
    @Input() customerMobile?: string;
    @Input() customerName?: string;
    @Input() mode: 'customer' | 'store' = 'customer';
    @Input() height: string = '380px';
    @Input() title: string = '';
    @Input() subtitle: string = '';

    @Output() messageSent = new EventEmitter<CustomerChatMessageDto>();
    @Output() unreadCountChange = new EventEmitter<number>();

    @ViewChild('messageScroll') private messageScroll?: ElementRef<HTMLDivElement>;

    messages: CustomerChatMessageDto[] = [];
    newMessage: string = '';
    sending: boolean = false;
    loading: boolean = false;
    isConnected: boolean = false;
    unreadCount: number = 0;

    private shouldScrollToBottom: boolean = false;
    private subscriptions: Subscription[] = [];
    private guestSessionId: string = '';

    get effectiveSessionId(): string {
        return this.sessionId || this.guestSessionId;
    }

    // Quick canned chips
    customerQuickChips = [
        'Call when arriving at door 📞',
        'Add extra sauces & napkins 🥫',
        'Is rider on the way? 🛵',
        'Please deliver quickly 🙏'
    ];

    storeQuickChips = [
        'Order is being prepared now! 👨‍🍳',
        'Rider is on the way! 🛵',
        'Ready in 5-10 minutes ⏱️',
        'Special instructions noted, thanks! 👍'
    ];

    constructor(private chatService: CustomerChatService) {}

    ngOnInit(): void {
        this.guestSessionId = this.chatService.getSessionId();

        // Listen for connection status
        this.subscriptions.push(
            this.chatService.isConnected$.subscribe(connected => {
                this.isConnected = connected;
            })
        );

        // Listen for incoming messages in real-time
        this.subscriptions.push(
            this.chatService.messageReceived$.subscribe(msg => {
                if (this.isMessageForThisContext(msg)) {
                    // Check if already in list to avoid duplicates
                    if (!this.messages.some(m => m.id === msg.id)) {
                        this.messages.push(msg);
                        this.shouldScrollToBottom = true;

                        // Check if sender is opposite side
                        const isOpposite = this.mode === 'customer'
                            ? msg.senderType === ChatMessageSenderType.Store
                            : msg.senderType === ChatMessageSenderType.Customer;

                        if (isOpposite) {
                            this.playNotificationSound();
                            this.markAsRead();
                        }
                    }
                }
            })
        );

        // Initialize chat session/groups
        this.initChat();
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['orderNo'] || changes['branchId'] || changes['sessionId']) {
            this.initChat();
        }
    }

    ngAfterViewChecked(): void {
        if (this.shouldScrollToBottom) {
            this.scrollToBottom();
            this.shouldScrollToBottom = false;
        }
    }

    ngOnDestroy(): void {
        if (this.orderNo) {
            this.chatService.leaveOrderChat(this.orderNo);
        }
        this.subscriptions.forEach(sub => sub.unsubscribe());
    }

    private isMessageForThisContext(msg: CustomerChatMessageDto): boolean {
        if (this.orderNo && msg.orderNo) {
            return msg.orderNo.trim().toUpperCase() === this.orderNo.trim().toUpperCase();
        }
        if (!this.orderNo && msg.sessionId && this.effectiveSessionId) {
            return msg.sessionId === this.effectiveSessionId;
        }
        return false;
    }

    async initChat(): Promise<void> {
        this.loading = true;
        try {
            if (this.orderNo) {
                const cleanNo = this.orderNo.trim();
                await this.chatService.joinOrderChat(cleanNo);
                this.messages = await this.chatService.getMessagesByOrderNo(cleanNo);
                await this.markAsRead();
            } else if (this.effectiveSessionId) {
                await this.chatService.joinSessionChat(this.effectiveSessionId);
                this.messages = await this.chatService.getMessagesBySession(this.effectiveSessionId);
                await this.markAsRead();
            }
            this.shouldScrollToBottom = true;
        } catch (err) {
            console.warn('Failed to load chat messages:', err);
        } finally {
            this.loading = false;
        }
    }

    async sendMessage(): Promise<void> {
        const text = this.newMessage.trim();
        if (!text || this.sending) return;

        this.sending = true;
        try {
            const senderType = this.mode === 'customer'
                ? ChatMessageSenderType.Customer
                : ChatMessageSenderType.Store;

            const senderName = this.mode === 'customer'
                ? (this.customerName || 'Customer')
                : 'Store Staff';

            const input: SendCustomerChatMessageInput = {
                orderNo: this.orderNo ? this.orderNo.trim().toUpperCase() : undefined,
                branchId: this.branchId,
                sessionId: !this.orderNo ? this.effectiveSessionId : undefined,
                senderName: senderName,
                customerMobile: this.customerMobile,
                senderType: senderType,
                message: text
            };

            const created = await this.chatService.sendMessage(input);
            if (created && !this.messages.some(m => m.id === created.id)) {
                this.messages.push(created);
            }

            this.newMessage = '';
            this.shouldScrollToBottom = true;
            this.messageSent.emit(created);
        } catch (err) {
            console.error('Failed to send chat message:', err);
        } finally {
            this.sending = false;
        }
    }

    applyQuickChip(chipText: string): void {
        this.newMessage = chipText;
        this.sendMessage();
    }

    async markAsRead(): Promise<void> {
        try {
            const isStore = this.mode === 'store';
            if (this.orderNo) {
                await this.chatService.markOrderMessagesAsRead(this.orderNo, isStore);
            } else if (this.effectiveSessionId) {
                await this.chatService.markSessionMessagesAsRead(this.effectiveSessionId, this.branchId, isStore);
            }
            this.unreadCount = 0;
            this.unreadCountChange.emit(0);
        } catch (err) {
            console.warn('Failed to mark messages as read:', err);
        }
    }

    isMyMessage(msg: CustomerChatMessageDto): boolean {
        if (this.mode === 'customer') {
            return msg.senderType === ChatMessageSenderType.Customer;
        } else {
            return msg.senderType === ChatMessageSenderType.Store;
        }
    }

    formatTime(time: string | Date): string {
        if (!time) return '';
        const d = new Date(time);
        return d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    }

    private scrollToBottom(): void {
        if (this.messageScroll) {
            try {
                this.messageScroll.nativeElement.scrollTop = this.messageScroll.nativeElement.scrollHeight;
            } catch {}
        }
    }

    private playNotificationSound(): void {
        try {
            const AudioContextClass = window.AudioContext || (window as any).webkitAudioContext;
            if (!AudioContextClass) return;
            const ctx = new AudioContextClass();
            const osc = ctx.createOscillator();
            const gain = ctx.createGain();

            osc.type = 'sine';
            osc.frequency.setValueAtTime(587.33, ctx.currentTime); // D5
            osc.frequency.exponentialRampToValueAtTime(880, ctx.currentTime + 0.15); // A5

            gain.gain.setValueAtTime(0.12, ctx.currentTime);
            gain.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + 0.25);

            osc.connect(gain);
            gain.connect(ctx.destination);
            osc.start();
            osc.stop(ctx.currentTime + 0.25);
        } catch {}
    }
}
