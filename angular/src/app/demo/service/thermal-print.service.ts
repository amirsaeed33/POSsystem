import { Injectable } from '@angular/core';
import { CustomerOrderDto } from '../api/customer-order';
import { BranchContextService } from './branch-context.service';
import { AuthService } from './auth.service';

@Injectable({ providedIn: 'root' })
export class ThermalPrintService {
    constructor(
        private branchContext: BranchContextService,
        private authService: AuthService
    ) {}

    printCustomerOrder(order: CustomerOrderDto, invoiceNo?: string): void {
        const branch = this.branchContext.getCurrentBranch();
        const branchName = branch?.name || order.branchName || 'Al Mezan Foods';
        const branchAddress = branch?.invoiceAddress || '';
        const branchPhone = branch?.invoiceContactPhone || '';

        const orderNum = order.orderNo || `#${order.id}`;
        const saleInvoice = invoiceNo || order.saleInvoiceNo || '';

        let orderDateStr = '';
        try {
            const d = new Date(order.orderDate);
            orderDateStr = d.toLocaleString();
        } catch {
            orderDateStr = String(order.orderDate);
        }

        const lines = order.lines || [];
        const itemsHtml =
            lines.length > 0
                ? lines
                      .map((l: any) => {
                          let dealHtml = '';
                          let modsHtml = '';
                          const modsRaw = l.selectedModifiers || l.modifiers;
                          let parsedMods: string[] = [];
                          if (modsRaw) {
                              try {
                                  const p = JSON.parse(modsRaw);
                                  parsedMods = Array.isArray(p) ? p : [modsRaw];
                              } catch {
                                  parsedMods = [modsRaw];
                              }
                          }
                          const dealMod = parsedMods.find((m) => typeof m === 'string' && m.startsWith('DEAL:'));
                          const dealSummary = (l.dealSummary || (dealMod ? dealMod.substring(5).trim() : '')) as string;
                          if (dealSummary) {
                              const items = dealSummary.includes(' • ') ? dealSummary.split(' • ') : dealSummary.split(',');
                              dealHtml = `
                                <div style="margin: 2px 0; padding-left: 4px; border-left: 2px solid #555; font-size: 10.5px; color: #222;">
                                    <div style="font-size: 9px; font-weight: bold; color: #555;">INCLUDES:</div>
                                    ${items.map((i: string) => `<div>&bull; ${i.trim()}</div>`).join('')}
                                </div>
                              `;
                          }
                          const extraMods = parsedMods.filter((m) => typeof m === 'string' && !m.startsWith('DEAL:'));
                          if (extraMods.length) {
                              modsHtml = `<div style="font-size: 10px; color: #555;">Options: ${extraMods.join(', ')}</div>`;
                          }
                          const isDeal = !!dealSummary || (l.productName || '').toLowerCase().includes('deal');
                          const dealBadge = isDeal
                              ? `<span style="background: #000; color: #fff; font-size: 8.5px; padding: 1px 3px; border-radius: 2px; font-weight: bold; margin-right: 3px;">DEAL</span>`
                              : '';
                          return `
                            <tr style="border-bottom: 1px dotted #ccc;">
                                <td style="vertical-align: top; padding: 4px 0; font-weight: bold; width: 14%;">${l.quantity}x</td>
                                <td style="vertical-align: top; padding: 4px 0; width: 50%;">
                                    <div style="font-weight: 500;">${dealBadge}${l.productName || 'Product #' + l.productId}</div>
                                    ${dealHtml}
                                    ${modsHtml}
                                    ${l.notes ? `<div style="font-size: 10px; color: #b91c1c; font-style: italic;">Note: ${l.notes}</div>` : ''}
                                </td>
                                <td style="vertical-align: top; text-align: right; padding: 4px 0; width: 16%;">${Number(l.unitPrice).toFixed(2)}</td>
                                <td style="vertical-align: top; text-align: right; padding: 4px 0; font-weight: 600; width: 20%;">${Number(l.lineTotal || l.quantity * l.unitPrice).toFixed(2)}</td>
                            </tr>
                          `;
                      })
                      .join('')
                : `<tr><td colspan="4" style="text-align: center; padding: 6px 0; color: #666;">No items listed</td></tr>`;

        const subTotal = order.subTotal || 0;
        const deliveryFee = order.deliveryFee || 0;
        const totalAmount = order.totalAmount || subTotal + deliveryFee;

        const customerInfoHtml = `
            <div style="border: 1px dashed #333; padding: 6px; margin: 6px 0; font-size: 12px; background: #fafafa;">
                <div style="font-weight: bold; margin-bottom: 3px; font-size: 13px;">CUSTOMER DETAILS</div>
                <div><strong>Name:</strong> ${order.customerName || 'Customer'}</div>
                ${order.customerPhone ? `<div><strong>Phone:</strong> ${order.customerPhone}</div>` : ''}
                ${order.deliveryAreaName ? `<div><strong>Area:</strong> ${order.deliveryAreaName}</div>` : ''}
                ${order.customerAddress ? `<div><strong>Address:</strong> ${order.customerAddress}</div>` : ''}
                ${order.notes ? `<div style="margin-top: 3px; font-style: italic;"><strong>Note:</strong> ${order.notes}</div>` : ''}
            </div>
        `;

        const receiptHtml = `
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8">
                <title>Receipt - ${orderNum}</title>
                <style>
                    @page {
                        size: 80mm auto;
                        margin: 0;
                    }
                    @media print {
                        body {
                            margin: 0;
                            padding: 6px;
                            width: 78mm;
                        }
                    }
                    body {
                        font-family: 'Courier New', Courier, monospace;
                        font-size: 12.5px;
                        line-height: 1.35;
                        color: #000;
                        background: #fff;
                        width: 78mm;
                        max-width: 78mm;
                        margin: 0 auto;
                        padding: 8px;
                        box-sizing: border-box;
                    }
                    .text-center { text-align: center; }
                    .text-right { text-align: right; }
                    .bold { font-weight: bold; }
                    .divider { border-top: 1px dashed #000; margin: 6px 0; }
                    .double-divider { border-top: 2px solid #000; margin: 6px 0; }
                    .row { display: flex; justify-content: space-between; font-size: 12px; margin-bottom: 2px; }
                    table { width: 100%; border-collapse: collapse; margin: 4px 0; }
                    th { border-bottom: 1px dashed #000; padding: 4px 0; font-size: 12px; font-weight: bold; }
                </style>
            </head>
            <body>
                <div class="text-center">
                    <div style="font-size: 16px; font-weight: bold;">${branchName}</div>
                    ${branchAddress ? `<div style="font-size: 11px;">${branchAddress}</div>` : ''}
                    ${branchPhone ? `<div style="font-size: 11px;">Phone: ${branchPhone}</div>` : ''}
                    <div class="divider"></div>
                    <div style="display: inline-block; border: 1px solid #000; padding: 2px 8px; font-weight: bold; font-size: 13px; margin: 4px 0;">
                        *** ONLINE ORDER ***
                    </div>
                </div>

                <div class="row" style="margin-top: 6px;">
                    <span class="bold">Order #:</span>
                    <span class="bold">${orderNum}</span>
                </div>
                ${
                    saleInvoice
                        ? `
                <div class="row">
                    <span class="bold">Invoice #:</span>
                    <span>${saleInvoice}</span>
                </div>`
                        : ''
                }
                <div class="row">
                    <span>Date:</span>
                    <span>${orderDateStr}</span>
                </div>
                <div class="row">
                    <span>Status:</span>
                    <span class="bold" style="color: #15803d;">APPROVED</span>
                </div>
                ${
                    order.riderName
                        ? `
                <div class="row" style="background: #f1f5f9; padding: 2px 4px; border-radius: 3px;">
                    <span class="bold">Delivery Rider:</span>
                    <span class="bold">${order.riderName}${order.riderPhone ? ' (' + order.riderPhone + ')' : ''}</span>
                </div>`
                        : ''
                }
                ${
                    (branch?.businessType === 2 || (order as any).businessType === 2)
                        ? `
                <div class="row" style="margin-top: 4px; font-weight: bold; border-top: 1px dotted #ccc; padding-top: 3px;">
                    <span>Printed By:</span>
                    <span>${this.authService.getPrinterIdentity().name} (${this.authService.getPrinterIdentity().role})</span>
                </div>
                <div class="row" style="font-size: 11px; color: #555;">
                    <span>Print Time:</span>
                    <span>${new Date().toLocaleTimeString()}</span>
                </div>`
                        : ''
                }

                ${customerInfoHtml}

                <table>
                    <thead>
                        <tr>
                            <th style="text-align: left; width: 14%;">Qty</th>
                            <th style="text-align: left; width: 50%;">Item</th>
                            <th style="text-align: right; width: 16%;">Price</th>
                            <th style="text-align: right; width: 20%;">Total</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${itemsHtml}
                    </tbody>
                </table>

                <div class="divider"></div>

                <div class="row">
                    <span>Subtotal:</span>
                    <span>PKR ${Number(subTotal).toFixed(2)}</span>
                </div>
                <div class="row">
                    <span>Delivery Fee:</span>
                    <span>${deliveryFee > 0 ? 'PKR ' + Number(deliveryFee).toFixed(2) : 'FREE'}</span>
                </div>

                <div class="row" style="font-size: 15px; font-weight: bold; border-top: 1px dashed #000; padding-top: 5px; margin-top: 5px;">
                    <span>TOTAL AMOUNT:</span>
                    <span>PKR ${Number(totalAmount).toFixed(2)}</span>
                </div>

                <div class="double-divider"></div>
                <div class="text-center" style="font-size: 11px; margin-top: 8px;">
                    <div>Thank you for ordering with us!</div>
                    <div style="margin-top: 4px;">* * * END OF RECEIPT * * *</div>
                </div>
            </body>
            </html>
        `;

        const win = window.open('', '_blank', 'height=650,width=420');
        if (!win) {
            console.warn('Popup blocked, falling back to window.print');
            window.print();
            return;
        }

        win.document.open();
        win.document.write(receiptHtml);
        win.document.close();
        win.focus();
        setTimeout(() => {
            try {
                win.print();
                win.close();
            } catch (e) {
                console.error('Print failed', e);
            }
        }, 350);
    }
}
