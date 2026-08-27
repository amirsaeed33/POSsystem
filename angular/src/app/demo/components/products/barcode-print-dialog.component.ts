import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { ProductDto } from 'src/app/demo/api/product';
import * as JsBarcode from 'jsbarcode';

export interface BarcodePrintItem {
    product: ProductDto;
    quantity: number;
}

@Component({
    selector: 'app-barcode-print-dialog',
    templateUrl: './barcode-print-dialog.component.html',
    styleUrls: ['./barcode-print-dialog.component.scss']
})
export class BarcodePrintDialogComponent implements OnChanges {
    @Input() visible = false;
    @Input() product: ProductDto | null = null;
    @Input() selectedProducts: ProductDto[] = [];
    @Output() visibleChange = new EventEmitter<boolean>();

    printItems: BarcodePrintItem[] = [];
    
    // Label customization settings
    showProductName = true;
    showPrice = true;
    showCodeText = true;
    showCompanyName = true;
    companyName = 'SMART POS';
    
    // Preset sticker size: 'thermal-single' (50x25mm), 'thermal-small' (38x25mm), 'a4-sheet' (3 columns)
    paperSize = 'thermal-single';

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['visible'] && this.visible) {
            this.initItems();
            setTimeout(() => this.generateBarcodes(), 200);
        }
    }

    initItems(): void {
        this.printItems = [];
        if (this.product) {
            this.printItems.push({ product: this.product, quantity: 5 });
        } else if (this.selectedProducts?.length) {
            this.printItems = this.selectedProducts.map(p => ({ product: p, quantity: 2 }));
        }
        this.updateStickers();
    }

    stickers: { product: ProductDto; idx: number; i: number }[] = [];

    updateStickers(): void {
        this.stickers = [];
        this.printItems.forEach((item, idx) => {
            const count = item.quantity || 1;
            for (let i = 0; i < count; i++) {
                this.stickers.push({ product: item.product, idx, i });
            }
        });
        setTimeout(() => this.generateBarcodes(), 100);
    }

    onQuantityChange(): void {
        this.updateStickers();
    }

    generateBarcodes(): void {
        this.printItems.forEach((item, index) => {
            const barcodeVal = item.product.barcode || item.product.id?.toString() || '000000';
            const previewId = `barcode-svg-${index}`;
            const svgElement = document.getElementById(previewId);
            if (svgElement) {
                try {
                    JsBarcode(`#${previewId}`, barcodeVal, {
                        format: 'CODE128',
                        lineColor: '#000',
                        width: 1.6,
                        height: 40,
                        displayValue: this.showCodeText,
                        fontSize: 12,
                        margin: 2
                    });
                } catch (e) {
                    console.warn('JsBarcode render failed for preview', barcodeVal, e);
                }
            }
        });

        this.stickers.forEach((st) => {
            const barcodeVal = st.product.barcode || st.product.id?.toString() || '000000';
            const printId = `barcode-print-svg-${st.idx}-${st.i}`;
            const printSvgElement = document.getElementById(printId);
            if (printSvgElement) {
                try {
                    JsBarcode(`#${printId}`, barcodeVal, {
                        format: 'CODE128',
                        lineColor: '#000',
                        width: 1.5,
                        height: 38,
                        displayValue: this.showCodeText,
                        fontSize: 11,
                        margin: 1
                    });
                } catch (e) {
                    console.warn('JsBarcode render failed for sticker', barcodeVal, e);
                }
            }
        });
    }

    get totalLabelsCount(): number {
        return this.printItems.reduce((acc, item) => acc + (item.quantity || 0), 0);
    }

    onClose(): void {
        this.visible = false;
        this.visibleChange.emit(false);
    }

    print(): void {
        document.body.classList.add('printing-barcode');
        this.updateStickers();
        setTimeout(() => {
            this.generateBarcodes();
            requestAnimationFrame(() => {
                window.print();
                setTimeout(() => {
                    document.body.classList.remove('printing-barcode');
                }, 500);
            });
        }, 150);
    }
}
