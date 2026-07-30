import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { EmailTemplateListComponent } from './email-template-list.component';

@NgModule({
    imports: [
        RouterModule.forChild([
            {
                path: '',
                component: EmailTemplateListComponent,
                data: { breadcrumb: 'Email Templates' },
            },
        ]),
    ],
    exports: [RouterModule],
})
export class EmailTemplatesRoutingModule {}
