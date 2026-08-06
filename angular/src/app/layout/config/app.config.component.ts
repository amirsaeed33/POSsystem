import { Component, Input, OnInit } from '@angular/core';
import { MessageService } from 'primeng/api';
import { LanguageInfo } from 'src/app/demo/api/localization';
import { LocalizationService } from 'src/app/demo/service/localization.service';
import {
    ColorScheme,
    LayoutService,
    MenuMode,
} from '../service/app.layout.service';
import { MenuService } from '../app.menu.service';

@Component({
    selector: 'app-config',
    templateUrl: './app.config.component.html',
    providers: [MessageService],
})
export class AppConfigComponent implements OnInit {
    @Input() minimal: boolean = false;

    componentThemes: any[] = [];

    scales: number[] = [12, 13, 14, 15, 16];

    languages: LanguageInfo[] = [];
    selectedLanguage = '';
    changingLanguage = false;

    get currentTheme(): string {
        return this.layoutService.config().theme;
    }
    set currentTheme(_val: string) {
        this.layoutService.config.update((config) => ({
            ...config,
            theme: _val,
        }));
    }

    get colorScheme(): ColorScheme {
        return this.layoutService.config().colorScheme;
    }
    set colorScheme(_val: ColorScheme) {
        if (_val == 'dark') {
            this.layoutService.config.update((config) => ({
                ...config,
                layoutTheme: 'colorScheme',
            }));
        }
        this.layoutService.config.update((config) => ({
            ...config,
            colorScheme: _val,
        }));
    }

    get visible(): boolean {
        return this.layoutService.state.configSidebarVisible;
    }
    set visible(_val: boolean) {
        this.layoutService.state.configSidebarVisible = _val;
    }

    get scale(): number {
        return this.layoutService.config().scale;
    }
    set scale(_val: number) {
        this.layoutService.config.update((config) => ({
            ...config,
            scale: _val,
        }));
    }

    get menuTheme(): string {
        return this.layoutService.config().layoutTheme;
    }
    set menuTheme(_val: string) {
        this.layoutService.config.update((config) => ({
            ...config,
            layoutTheme: _val,
        }));
    }

    get menuMode(): MenuMode {
        return this.layoutService.config().menuMode;
    }
    set menuMode(_val: MenuMode) {
        this.layoutService.config.update((config) => ({
            ...config,
            menuMode: _val,
        }));
        this.layoutService.state.staticMenuDesktopInactive = false;
        this.layoutService.state.overlayMenuActive = false;
        this.layoutService.state.staticMenuMobileActive = false;
        this.layoutService.state.menuHoverActive = false;
        if (this.layoutService.isSlimPlus() || this.layoutService.isSlim()) {
            this.menuService.reset();
        }
    }

    get inputStyle(): string {
        return this.layoutService.config().inputStyle;
    }
    set inputStyle(_val: string) {
        this.layoutService.config.update((config) => ({
            ...config,
            inputStyle: _val,
        }));
    }

    get ripple(): boolean {
        return this.layoutService.config().ripple;
    }
    set ripple(_val: boolean) {
        this.layoutService.config.update((config) => ({
            ...config,
            menuTheme: _val,
        }));
    }

    constructor(
        public layoutService: LayoutService,
        public menuService: MenuService,
        private localizationService: LocalizationService,
        private messageService: MessageService
    ) {}

    ngOnInit() {
        this.componentThemes = [
            { name: 'indigo', lightColor: '#4C63B6', darkColor: '#6A7EC2' },
            { name: 'blue', lightColor: '#1992D4', darkColor: '#3BABE8' },
            { name: 'green', lightColor: '#27AB83', darkColor: '#44D4A9' },
            { name: 'deeppurple', lightColor: '#896FF4', darkColor: '#B1A0F8' },
            { name: 'orange', lightColor: '#DE911D', darkColor: '#E8AB4F' },
            { name: 'cyan', lightColor: '#00B9C6', darkColor: '#58CDD5' },
            { name: 'yellow', lightColor: '#F9C404', darkColor: '#FDDD68' },
            { name: 'pink', lightColor: '#C74B95', darkColor: '#D77FB4' },
            { name: 'purple', lightColor: '#BA6FF4', darkColor: '#D1A0F8' },
            { name: 'lime', lightColor: '#84BD20', darkColor: '#A3D44E' },
        ];

        this.localizationService
            .ensureLoaded()
            .catch(() => undefined)
            .finally(() => {
                this.languages = this.localizationService.getLanguages();
                this.selectedLanguage =
                    this.localizationService.getCurrentLanguage()?.name || '';
            });
    }

    onChangeLanguage(languageName: string): void {
        if (
            !languageName ||
            languageName === this.localizationService.getCurrentLanguage()?.name ||
            this.changingLanguage
        ) {
            return;
        }

        this.changingLanguage = true;
        this.localizationService.changeLanguage(languageName).catch((error) => {
            this.changingLanguage = false;
            this.selectedLanguage =
                this.localizationService.getCurrentLanguage()?.name || '';
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: error?.message || 'Failed to change language',
            });
        });
    }

    changeTheme(theme: string) {
        this.currentTheme = theme;
    }

    isIE() {
        return /(MSIE|Trident\/|Edge\/)/i.test(window.navigator.userAgent);
    }

    onConfigButtonClick() {
        this.layoutService.showConfigSidebar();
    }

    decrementScale() {
        this.scale--;
    }

    incrementScale() {
        this.scale++;
    }
}
