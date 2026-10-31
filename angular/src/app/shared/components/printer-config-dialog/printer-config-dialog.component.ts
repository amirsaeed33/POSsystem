import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { ThermalPaperSize, ThermalPrintConfig, ThermalPrintService } from 'src/app/demo/service/thermal-print.service';
import { MessageService } from 'primeng/api';

@Component({
    selector: 'app-printer-config-dialog',
    templateUrl: './printer-config-dialog.component.html',
    styleUrls: ['./printer-config-dialog.component.scss'],
})
export class PrinterConfigDialogComponent implements OnInit {
    @Input() visible = false;
    @Output() visibleChange = new EventEmitter<boolean>();

    config: ThermalPrintConfig = {
        paperSize: '80mm',
        autoPrint: true,
        copies: 1,
        showLogo: true,
        showPreview: false,
        headerText: '',
        footerMessage: '',
    };

    paperSizeOptions = [
        { label: '80mm Standard Thermal Roll (48 chars / ~72mm printable)', value: '80mm' },
        { label: '58mm Compact Thermal Roll (32 chars / ~48mm printable)', value: '58mm' },
    ];

    copiesOptions = [
        { label: '1 Copy (Standard)', value: 1 },
        { label: '2 Copies (Customer + Kitchen/Merchant)', value: 2 },
        { label: '3 Copies', value: 3 },
    ];

    constructor(
        private thermalPrintService: ThermalPrintService,
        private messageService: MessageService
    ) {}

    ngOnInit(): void {
        this.loadCurrentConfig();
        this.thermalPrintService.config$.subscribe((cfg) => {
            this.config = { ...cfg };
        });
    }

    loadCurrentConfig(): void {
        this.config = { ...this.thermalPrintService.getConfig() };
    }

    setPaperSize(size: ThermalPaperSize): void {
        this.config.paperSize = size;
        this.save();
    }

    save(): void {
        this.thermalPrintService.updateConfig(this.config);
        this.messageService.add({
            severity: 'success',
            summary: 'Printer Config Updated',
            detail: `Paper size set to ${this.config.paperSize}, auto-print: ${this.config.autoPrint ? 'ON' : 'OFF'}`,
            life: 2500,
        });
    }

    saveAndClose(): void {
        this.save();
        this.onClose();
    }

    onClose(): void {
        this.visible = false;
        this.visibleChange.emit(false);
    }

    printTest(): void {
        this.save();
        this.thermalPrintService.printTestReceipt(this.config.paperSize);
    }
}

