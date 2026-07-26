import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { LayoutService } from 'src/app/layout/service/app.layout.service';
import { AuthService } from 'src/app/demo/service/auth.service';
import { SessionService } from 'src/app/demo/service/session.service';
import { MessageService } from 'primeng/api';

@Component({
	templateUrl: './login.component.html',
	providers: [MessageService]
})
export class LoginComponent implements OnInit {

	loginForm: FormGroup;
	loading = false;
	private returnUrl = '/';

	constructor(
		private layoutService: LayoutService,
		private fb: FormBuilder,
		private authService: AuthService,
		private sessionService: SessionService,
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

	ngOnInit(): void {
		this.returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/';

		if (this.authService.isAuthenticated()) {
			this.router.navigateByUrl(this.returnUrl);
		}
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
			.then(() => this.sessionService.getCurrentLoginInformations())
			.then((sessionInfo) => {
				if (sessionInfo?.user) {
					this.authService.setUserInfo(sessionInfo.user);
				}
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
}
