import { Component, NgZone, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { LayoutService } from 'src/app/layout/service/app.layout.service';
import { AuthService } from 'src/app/demo/service/auth.service';
import { PermissionService } from 'src/app/demo/service/permission.service';
import { SessionService } from 'src/app/demo/service/session.service';
import { TenantContextService } from 'src/app/demo/service/tenant-context.service';
import { TenantAvailabilityState } from 'src/app/demo/api/account';
import { MessageService } from 'primeng/api';

declare const google: any;

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
	googleEnabled = false;
	private googleClientId = '';
	private googleScriptLoaded = false;
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
		private messageService: MessageService,
		private ngZone: NgZone
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
			await this.initGoogleSignIn();
		} catch {
			// Continue with login even if tenant/google config fails.
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
			.then(() => this.completeLogin())
			.catch((error) => this.showLoginError(error))
			.finally(() => {
				this.loading = false;
			});
	}

	loginWithGoogle(event?: Event): void {
		event?.preventDefault();

		if (!this.googleEnabled || !this.googleClientId) {
			this.messageService.add({
				severity: 'warn',
				summary: 'Google Sign-In',
				detail: 'Set Authentication:Google:ClientId in appsettings.json (Google Cloud OAuth Web client ID), then restart the API.',
			});
			return;
		}

		if (typeof google === 'undefined' || !google?.accounts?.oauth2) {
			this.messageService.add({
				severity: 'error',
				summary: 'Google Sign-In',
				detail: 'Google script is not loaded yet. Please try again.',
			});
			return;
		}

		const tokenClient = google.accounts.oauth2.initTokenClient({
			client_id: this.googleClientId,
			scope: 'openid email profile',
			callback: (tokenResponse: any) => {
				this.ngZone.run(() => this.handleGoogleTokenResponse(tokenResponse));
			},
			error_callback: (error: any) => {
				this.ngZone.run(() => {
					this.messageService.add({
						severity: 'error',
						summary: 'Google Sign-In',
						detail: error?.message || 'Google sign-in was cancelled or failed.',
					});
				});
			},
		});

		tokenClient.requestAccessToken({ prompt: 'select_account' });
	}

	private async handleGoogleTokenResponse(tokenResponse: any): Promise<void> {
		if (!tokenResponse?.access_token) {
			this.messageService.add({
				severity: 'error',
				summary: 'Google Sign-In',
				detail: tokenResponse?.error || 'No access token returned from Google.',
			});
			return;
		}

		this.loading = true;
		try {
			await this.authService.externalAuthenticate({
				authProvider: 'Google',
				providerAccessCode: tokenResponse.access_token,
			});
			await this.completeLogin();
		} catch (error: any) {
			this.showLoginError(error);
		} finally {
			this.loading = false;
		}
	}

	private async completeLogin(): Promise<void> {
		const sessionInfo = await this.sessionService.getCurrentLoginInformations();
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

		await this.permissionService.load();
		this.messageService.add({
			severity: 'success',
			summary: 'Success',
			detail: 'Login successful',
		});
		this.router.navigateByUrl(this.returnUrl);
	}

	private showLoginError(error: any): void {
		const errorMessage =
			error?.message ||
			'Login failed. Please check your credentials.';
		this.messageService.add({
			severity: 'error',
			summary: 'Error',
			detail: errorMessage,
		});
	}

	private async initGoogleSignIn(): Promise<void> {
		const providers = await this.authService.getExternalAuthenticationProviders();
		const googleProvider = providers.find(
			(p) => (p.name || '').toLowerCase() === 'google'
		);
		if (!googleProvider?.clientId) {
			this.googleEnabled = false;
			return;
		}

		this.googleClientId = googleProvider.clientId;
		await this.loadGoogleScript();
		this.googleEnabled = true;
	}

	private loadGoogleScript(): Promise<void> {
		if (this.googleScriptLoaded || (typeof google !== 'undefined' && google?.accounts)) {
			this.googleScriptLoaded = true;
			return Promise.resolve();
		}

		return new Promise((resolve, reject) => {
			const existing = document.getElementById('google-gsi-client');
			if (existing) {
				existing.addEventListener('load', () => resolve());
				existing.addEventListener('error', () => reject(new Error('Failed to load Google script')));
				return;
			}

			const script = document.createElement('script');
			script.id = 'google-gsi-client';
			script.src = 'https://accounts.google.com/gsi/client';
			script.async = true;
			script.defer = true;
			script.onload = () => {
				this.googleScriptLoaded = true;
				resolve();
			};
			script.onerror = () => reject(new Error('Failed to load Google script'));
			document.head.appendChild(script);
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
