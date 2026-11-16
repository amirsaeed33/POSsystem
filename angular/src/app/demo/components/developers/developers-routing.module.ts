import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { DeveloperListComponent } from './developer-list.component';

@NgModule({
    imports: [
        RouterModule.forChild([{ path: '', component: DeveloperListComponent }]),
    ],
    exports: [RouterModule],
})
export class DevelopersRoutingModule {}
