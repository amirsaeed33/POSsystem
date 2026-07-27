import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { LayoutService } from 'src/app/layout/service/app.layout.service';
import { AuthService } from 'src/app/demo/service/auth.service';
import { PermissionService } from 'src/app/demo/service/permission.service';
import { SessionService } from 'src/app/demo/service/session.service';
import { TenantContextService } from 'src/app/demo/service/tenant-context.service';
import { TenantAvailabilityState } from 'src/app/demo/api/account';
import { MessageService } from 'primeng/api';

@Component({
	templateUrl: './login.component.html',
	providers: [MessageService]
})
export class LoginComponent implements OnInit {

	loginForm: FormGroup;
	loading = false;
	tenantReady = false;
	tenancyName = '';
	tenantDisplayName = '';
	private returnUrl = '/';

	constructor(
		private layoutService: LayoutService,
		private fb: FormBuilder,
		private authService: AuthService,
		private sessionService: SessionService,
		private permissionService: PermissionService,
		private tenantContext: TenantContextService,
		private router: Router,
		private route: ActivatedRoute,
		private messageService: MessageService
	) {
		this.loginForm = this.fb.group({
			userNameOrEmailAddress: ['', [Validators.required]],
			password: ['', [Validators.required]],
			rememberClient: [false]
		});
	}

	async ngOnInit(): Promise<void> {
		this.returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/';

		try {
			await this.tenantContext.ensureMultiTenancyLoaded();
			await this.resolveTenancyFromQueryString();
			await this.refreshTenantDisplay();
		} catch {
			// Continue with login even if tenant config fails.
		} finally {
			this.tenantReady = true;
		}

		if (this.authService.isAuthenticated()) {
			this.router.navigateByUrl(this.returnUrl);
		}
	}

	get filledInput(): boolean {
		return this.layoutService.config().inputStyle === 'filled';
	}

	get isMultiTenancyEnabled(): boolean {
		return this.tenantContext.isMultiTenancyEnabled();
	}

	onSubmit(): void {
		if (this.loginForm.invalid) {
			this.loginForm.markAllAsTouched();
			return;
		}

		this.loading = true;
		const model = this.loginForm.value;

		this.authService.authenticate(model)
			.then(() => this.sessionService.getCurrentLoginInformations())
			.then((sessionInfo) => {
				if (sessionInfo?.user) {
					this.authService.setUserInfo(sessionInfo.user);
				}
				if (sessionInfo?.tenant) {
					this.tenantContext.setTenantInfo(sessionInfo.tenant);
					if (sessionInfo.tenant.id) {
						this.tenantContext.setTenantId(sessionInfo.tenant.id);
					}
				} else {
					this.tenantContext.setTenantInfo(null);
				}
				return this.permissionService.load();
			})
			.then(() => {
				this.messageService.add({
					severity: 'success',
					summary: 'Success',
					detail: 'Login successful',
				});
				this.router.navigateByUrl(this.returnUrl);
			})
			.catch((error) => {
				const errorMessage =
					error?.message ||
					'Login failed. Please check your credentials.';
				this.messageService.add({
					severity: 'error',
					summary: 'Error',
					detail: errorMessage,
				});
			})
			.finally(() => {
				this.loading = false;
			});
	}

	private async resolveTenancyFromQueryString(): Promise<void> {
		const tenancyName = this.route.snapshot.queryParams['abp_tenancy_name'];
		if (!tenancyName || typeof tenancyName !== 'string') {
			return;
		}

		const result = await this.tenantContext.resolveTenancyName(tenancyName);
		if (result.state === TenantAvailabilityState.Available && result.changed) {
			location.reload();
			return;
		}
		if (result.state === TenantAvailabilityState.InActive) {
			this.messageService.add({
				severity: 'warn',
				summary: 'Warning',
				detail: `Tenant "${tenancyName}" is not active.`,
			});
		} else if (result.state === TenantAvailabilityState.NotFound) {
			this.messageService.add({
				severity: 'warn',
				summary: 'Warning',
				detail: `There is no tenant defined with name ${tenancyName}.`,
			});
		}
	}

	private async refreshTenantDisplay(): Promise<void> {
		const cached = this.tenantContext.getTenantInfo();
		if (cached?.tenancyName) {
			this.tenancyName = cached.tenancyName;
			this.tenantDisplayName = cached.name || cached.tenancyName;
		}

		if (!this.tenantContext.getTenantId()) {
			this.tenancyName = '';
			this.tenantDisplayName = '';
			this.tenantContext.setTenantInfo(null);
			return;
		}

		try {
			const sessionInfo = await this.sessionService.getCurrentLoginInformations();
			if (sessionInfo?.tenant) {
				this.tenantContext.setTenantInfo(sessionInfo.tenant);
				this.tenancyName = sessionInfo.tenant.tenancyName || '';
				this.tenantDisplayName =
					sessionInfo.tenant.name || sessionInfo.tenant.tenancyName || '';
			}
		} catch {
			// Keep cookie / cached tenant display.
		}
	}
}
