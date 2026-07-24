import { NgModule } from '@angular/core';
import { SharedModule } from '@shared/shared.module';
import { UnitsRoutingModule } from './units-routing.module';
import { CreateUnitDialogComponent } from './create-unit/create-unit-dialog.component';
import { EditUnitDialogComponent } from './edit-unit/edit-unit-dialog.component';
import { UnitsComponent } from './units.component';

@NgModule({
    declarations: [
        CreateUnitDialogComponent,
        EditUnitDialogComponent,
        UnitsComponent,
    ],
    imports: [SharedModule, UnitsRoutingModule],
})
export class UnitsModule {}
