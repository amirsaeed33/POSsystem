import { animate, style, transition, trigger } from '@angular/animations';
import { Component, NgZone, OnDestroy, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { LayoutService } from 'src/app/layout/service/app.layout.service';
import { AuthService } from 'src/app/demo/service/auth.service';
import { PermissionService } from 'src/app/demo/service/permission.service';
import { SessionService } from 'src/app/demo/service/session.service';
import { TenantContextService } from 'src/app/demo/service/tenant-context.service';
import { BranchContextService } from 'src/app/demo/service/branch-context.service';
import { MessageService } from 'primeng/api';

declare const google: any;

type LoginMode = 'password' | 'emailCode';

@Component({
	templateUrl: './login.component.html',
	providers: [MessageService],
	animations: [
		trigger('slideIn', [
			transition(':enter', [
				style({ transform: 'translateX(-48px)', opacity: 0 }),
				animate(
					'420ms cubic-bezier(0.22, 1, 0.36, 1)',
					style({ transform: 'translateX(0)', opacity: 1 })
				),
			]),
		]),
	],
})
export class LoginComponent implements OnInit, OnDestroy {

	loginForm: FormGroup;
	emailCodeForm: FormGroup;
	loading = false;
	googleEnabled = false;
	loginMode: LoginMode = 'password';
	emailCodeSent = false;
	emailCodeExpirationMinutes = 5;
	resendCooldownRemaining = 0;
	private googleClientId = '';
	private googleScriptLoaded = false;
	private returnUrl = '/';
	private resendTimer: ReturnType<typeof setInterval> | null = null;

	constructor(
		private layoutService: LayoutService,
		private fb: FormBuilder,
		private authService: AuthService,
		private sessionService: SessionService,
		private permissionService: PermissionService,
		private tenantContext: TenantContextService,
		private branchContext: BranchContextService,
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

		this.emailCodeForm = this.fb.group({
			emailAddress: ['', [Validators.required, Validators.email]],
			code: ['', [Validators.required, Validators.pattern(/^\d{6}$/)]]
		});
	}

	async ngOnInit(): Promise<void> {
		this.returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/';

		// Tenant is resolved from the user on the backend after login — clear any stale picker cookie.
		if (!this.authService.isAuthenticated()) {
			this.tenantContext.setTenantId(undefined);
		}

		try {
			await this.initGoogleSignIn();
		} catch {
			// Continue with login even if google config fails.
		}

		if (this.authService.isAuthenticated()) {
			this.router.navigateByUrl(this.returnUrl);
		}
	}

	ngOnDestroy(): void {
		this.clearResendTimer();
	}

	get filledInput(): boolean {
		return this.layoutService.config().inputStyle === 'filled';
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

	switchToEmailCodeLogin(event?: Event): void {
		event?.preventDefault();
		this.loginMode = 'emailCode';
		this.emailCodeSent = false;
		this.emailCodeForm.reset({ emailAddress: '', code: '' });
		this.clearResendTimer();
	}

	switchToPasswordLogin(): void {
		this.loginMode = 'password';
		this.emailCodeSent = false;
		this.emailCodeForm.reset({ emailAddress: '', code: '' });
		this.clearResendTimer();
	}

	resetEmailCodeFlow(): void {
		this.emailCodeSent = false;
		this.emailCodeForm.patchValue({ code: '' });
		this.emailCodeForm.get('code')?.markAsUntouched();
		this.clearResendTimer();
	}

	onEmailCodeSubmit(): void {
		if (!this.emailCodeSent) {
			this.sendEmailCode();
			return;
		}
		this.verifyEmailCode();
	}

	resendEmailCode(): void {
		if (this.resendCooldownRemaining > 0) {
			return;
		}
		this.sendEmailCode(true);
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

	private sendEmailCode(isResend = false): void {
		const emailControl = this.emailCodeForm.get('emailAddress');
		emailControl?.markAsTouched();
		if (emailControl?.invalid) {
			return;
		}

		this.loading = true;
		const email = (emailControl?.value || '').trim();

		this.authService
			.sendEmailLoginCode(email)
			.then((result) => {
				this.emailCodeSent = true;
				this.emailCodeExpirationMinutes = result.expirationMinutes || 5;
				this.startResendCooldown(result.resendCooldownSeconds || 60);
				this.messageService.add({
					severity: 'success',
					summary: 'Code sent',
					detail: isResend
						? 'A new sign-in code was sent to your email.'
						: `We sent a 6-digit code to ${email}.`,
				});
			})
			.catch((error) => this.showLoginError(error))
			.finally(() => {
				this.loading = false;
			});
	}

	private verifyEmailCode(): void {
		const codeControl = this.emailCodeForm.get('code');
		codeControl?.markAsTouched();
		if (this.emailCodeForm.get('emailAddress')?.invalid || codeControl?.invalid) {
			return;
		}

		this.loading = true;
		const email = (this.emailCodeForm.get('emailAddress')?.value || '').trim();
		const code = (codeControl?.value || '').trim();

		this.authService
			.authenticateWithEmailCode(email, code)
			.then(() => this.completeLogin())
			.catch((error) => this.showLoginError(error))
			.finally(() => {
				this.loading = false;
			});
	}

	private startResendCooldown(seconds: number): void {
		this.clearResendTimer();
		this.resendCooldownRemaining = Math.max(0, seconds);
		if (this.resendCooldownRemaining <= 0) {
			return;
		}

		this.resendTimer = setInterval(() => {
			this.resendCooldownRemaining = Math.max(0, this.resendCooldownRemaining - 1);
			if (this.resendCooldownRemaining <= 0) {
				this.clearResendTimer();
			}
		}, 1000);
	}

	private clearResendTimer(): void {
		if (this.resendTimer) {
			clearInterval(this.resendTimer);
			this.resendTimer = null;
		}
		this.resendCooldownRemaining = 0;
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
			BranchContextService.setHostAdminSession(false);
		} else {
			// Host admin: no host location — clear leftover context; topbar loads all locations.
			this.tenantContext.setTenantInfo(null);
			this.tenantContext.setTenantId(undefined);
			this.branchContext.clear();
			BranchContextService.setHostAdminSession(true);
		}

		await this.permissionService.load();

		if (sessionInfo?.tenant) {
			const branches = await this.branchContext.ensureLoaded(
				sessionInfo.user?.branchId ?? null
			);
			if (!branches?.length) {
				this.messageService.add({
					severity: 'success',
					summary: 'Success',
					detail: 'Login successful. Create your first branch to continue.',
				});
				this.router.navigateByUrl('/branches/create');
				return;
			}
		}

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

}
