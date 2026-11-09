import { NgModule } from '@angular/core';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { AppLayoutModule } from './layout/app.layout.module';
import { AuthInterceptor } from './demo/service/auth.interceptor';

@NgModule({
    imports: [
        AppLayoutModule,
        AppRoutingModule,
        HttpClientModule,
    ],
    declarations: [
        AppComponent,
    ],
    providers: [
        // PathLocationStrategy (default) — required for Google to index clean URLs.
        { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true },
    ],
    bootstrap: [AppComponent]
})
export class AppModule { }
