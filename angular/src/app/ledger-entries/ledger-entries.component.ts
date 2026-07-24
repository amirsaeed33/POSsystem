import { ChangeDetectorRef, Component, Injector, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { BsModalService, BsModalRef } from 'ngx-bootstrap/modal';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import {
  PagedListingComponentBase,
  PagedRequestDto,
} from '@shared/paged-listing-component-base';
import {
  LedgerEntryServiceProxy,
  LedgerEntryDto,
  LedgerEntryDtoPagedResultDto,
  BusinessAccountServiceProxy,
  BusinessAccountDto,
} from '@shared/service-proxies/service-proxies';
import { CreateLedgerEntryDialogComponent } from './create-ledger-entry/create-ledger-entry-dialog.component';
import { EditLedgerEntryDialogComponent } from './edit-ledger-entry/edit-ledger-entry-dialog.component';

class PagedLedgerEntriesRequestDto extends PagedRequestDto {
  keyword: string;
  accountId: number | undefined;
}

@Component({
  templateUrl: './ledger-entries.component.html',
  animations: [appModuleAnimation()]
})
export class LedgerEntriesComponent extends PagedListingComponentBase<LedgerEntryDto> implements OnInit {
  entries: LedgerEntryDto[] = [];
  accounts: BusinessAccountDto[] = [];
  keyword = '';
  accountId: number | undefined;

  constructor(
    injector: Injector,
    private _ledgerService: LedgerEntryServiceProxy,
    private _accountService: BusinessAccountServiceProxy,
    private _modalService: BsModalService,
    private _activatedRoute: ActivatedRoute,
    changeDetector: ChangeDetectorRef
  ) {
    super(injector, changeDetector);
  }

  ngOnInit(): void {
    const accountIdParam = this._activatedRoute.snapshot.queryParamMap.get('accountId');
    if (accountIdParam) {
      this.accountId = Number(accountIdParam);
    }

    this._accountService.getAll(undefined, undefined, 1000).subscribe((result) => {
      this.accounts = result.items;
      this.cd.detectChanges();
    });

    super.ngOnInit();
  }

  list(
    request: PagedLedgerEntriesRequestDto,
    pageNumber: number,
    finishedCallback: Function
  ): void {
    request.keyword = this.keyword;
    request.accountId = this.accountId;

    this._ledgerService
      .getAll(
        request.keyword,
        request.accountId,
        undefined,
        undefined,
        undefined,
        request.skipCount,
        request.maxResultCount
      )
      .pipe(
        finalize(() => {
          finishedCallback();
        })
      )
      .subscribe((result: LedgerEntryDtoPagedResultDto) => {
        this.entries = result.items;
        this.showPaging(result, pageNumber);
      });
  }

  delete(entry: LedgerEntryDto): void {
    abp.message.confirm(
      this.l('LedgerEntryDeleteWarningMessage'),
      undefined,
      (result: boolean) => {
        if (result) {
          this._ledgerService
            .delete(entry.id)
            .pipe(
              finalize(() => {
                abp.notify.success(this.l('SuccessfullyDeleted'));
                this.refresh();
              })
            )
            .subscribe(() => {});
        }
      }
    );
  }

  createEntry(): void {
    this.showCreateOrEditDialog();
  }

  editEntry(entry: LedgerEntryDto): void {
    this.showCreateOrEditDialog(entry.id);
  }

  showCreateOrEditDialog(id?: number): void {
    let dialog: BsModalRef;
    if (!id) {
      dialog = this._modalService.show(CreateLedgerEntryDialogComponent, {
        class: 'modal-lg',
        initialState: {
          accountId: this.accountId,
        },
      });
    } else {
      dialog = this._modalService.show(EditLedgerEntryDialogComponent, {
        class: 'modal-lg',
        initialState: {
          id: id,
        },
      });
    }

    dialog.content.onSave.subscribe(() => {
      this.refresh();
    });
  }
}
