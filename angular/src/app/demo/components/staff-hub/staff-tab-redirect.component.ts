import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

@Component({
    standalone: true,
    template: '',
})
export class StaffTabRedirectComponent implements OnInit {
    private readonly router = inject(Router);
    private readonly route = inject(ActivatedRoute);

    ngOnInit(): void {
        const tab = this.route.snapshot.data['tab'] as string;
        this.router.navigate(['/staff'], {
            queryParams: tab ? { tab } : {},
            replaceUrl: true,
        });
    }
}
