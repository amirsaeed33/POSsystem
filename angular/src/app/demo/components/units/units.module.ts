import { NgModule } from '@angular/core';
import { UnitsRoutingModule } from './units-routing.module';
import { UnitsSharedModule } from './units-shared.module';

@NgModule({
    imports: [UnitsSharedModule, UnitsRoutingModule],
})
export class UnitsModule {}
