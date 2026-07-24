import { ChangeDetectorRef, Component, Injector } from '@angular/core';
import { finalize } from 'rxjs/operators';
import { BsModalService, BsModalRef } from 'ngx-bootstrap/modal';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import {
  PagedListingComponentBase,
  PagedRequestDto,
} from '@shared/paged-listing-component-base';
import {
  BusinessAccountServiceProxy,
  BusinessAccountDto,
  BusinessAccountDtoPagedResultDto,
} from '@shared/service-proxies/service-proxies';
import { CreateAccountDialogComponent } from './create-account/create-account-dialog.component';
import { EditAccountDialogComponent } from './edit-account/edit-account-dialog.component';

class PagedAccountsRequestDto extends PagedRequestDto {
  keyword: string;
}

@Component({
  templateUrl: './accounts.component.html',
  animations: [appModuleAnimation()]
})
export class AccountsComponent extends PagedListingComponentBase<BusinessAccountDto> {
  accounts: BusinessAccountDto[] = [];
  keyword = '';

  constructor(
    injector: Injector,
    private _accountService: BusinessAccountServiceProxy,
    private _modalService: BsModalService,
    cd: ChangeDetectorRef
  ) {
    super(injector, cd);
  }

  list(
    request: PagedAccountsRequestDto,
    pageNumber: number,
    finishedCallback: Function
  ): void {
    request.keyword = this.keyword;

    this._accountService
      .getAll(
        request.keyword,
        request.skipCount,
        request.maxResultCount
      )
      .pipe(
        finalize(() => {
          finishedCallback();
        })
      )
      .subscribe((result: BusinessAccountDtoPagedResultDto) => {
        this.accounts = result.items;
        this.showPaging(result, pageNumber);
      });
  }

  delete(account: BusinessAccountDto): void {
    abp.message.confirm(
      this.l('AccountDeleteWarningMessage', account.name),
      undefined,
      (result: boolean) => {
        if (result) {
          this._accountService
            .delete(account.id)
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

  createAccount(): void {
    this.showCreateOrEditAccountDialog();
  }

  editAccount(account: BusinessAccountDto): void {
    this.showCreateOrEditAccountDialog(account.id);
  }

  showCreateOrEditAccountDialog(id?: number): void {
    let createOrEditAccountDialog: BsModalRef;
    if (!id) {
      createOrEditAccountDialog = this._modalService.show(
        CreateAccountDialogComponent,
        {
          class: 'modal-lg',
        }
      );
    } else {
      createOrEditAccountDialog = this._modalService.show(
        EditAccountDialogComponent,
        {
          class: 'modal-lg',
          initialState: {
            id: id,
          },
        }
      );
    }

    createOrEditAccountDialog.content.onSave.subscribe(() => {
      this.refresh();
    });
  }
}
