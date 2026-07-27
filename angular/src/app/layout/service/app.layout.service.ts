import { Injectable, effect, signal } from '@angular/core';
import { MenuItem } from 'primeng/api';
import { fromEvent, Subject } from 'rxjs';
import { debounceTime } from 'rxjs/operators';
import { TabCloseEvent } from '../api/tabcloseevent';

export type MenuMode = 'static' | 'overlay' | 'slim-plus' | 'slim';

export type ColorScheme = 'light' | 'dark';

export interface AppConfig {
    inputStyle: string;
    colorScheme: ColorScheme;
    theme: string;
    ripple: boolean;
    menuMode: MenuMode;
    layoutTheme: string;
    scale: number;
}

interface LayoutState {
    staticMenuDesktopInactive: boolean;
    overlayMenuActive: boolean;
    profileSidebarVisible: boolean;
    configSidebarVisible: boolean;
    staticMenuMobileActive: boolean;
    menuHoverActive: boolean;
}

@Injectable({
    providedIn: 'root',
})
export class LayoutService {
    private readonly desktopBreakpoint = 991;
    private readonly storageKey = 'verona-layout-config';

    /** Baseline matching index.html theme link (used for first theme swap detection). */
    _config: AppConfig = {
        ripple: false,
        inputStyle: 'outlined',
        menuMode: this.getDefaultMenuMode(),
        colorScheme: 'light',
        theme: 'indigo',
        layoutTheme: 'colorScheme',
        scale: 14,
    };

    config = signal<AppConfig>(this._config);

    state: LayoutState = {
        staticMenuDesktopInactive: false,
        overlayMenuActive: false,
        profileSidebarVisible: false,
        configSidebarVisible: false,
        staticMenuMobileActive: false,
        menuHoverActive: false,
    };

    tabs: MenuItem[] = [];

    private configUpdate = new Subject<AppConfig>();

    private overlayOpen = new Subject<any>();

    private tabOpen = new Subject<MenuItem>();

    private tabClose = new Subject<TabCloseEvent>();

    configUpdate$ = this.configUpdate.asObservable();

    overlayOpen$ = this.overlayOpen.asObservable();

    tabOpen$ = this.tabOpen.asObservable();

    tabClose$ = this.tabClose.asObservable();

    constructor() {
        const saved = this.loadConfig();
        if (saved) {
            // Keep _config as index.html baseline so updateStyle/changeTheme run on restore.
            this.config.set({ ...this._config, ...saved });
        }

        effect(() => {
            const config = this.config();
            if (this.updateStyle(config)) {
                this.changeTheme();
            }
            this.changeScale(config.scale);
            this.onConfigUpdate();
        });

        this.applyResponsiveMenuMode();
        fromEvent(window, 'resize')
            .pipe(debounceTime(100))
            .subscribe(() => this.applyResponsiveMenuMode());
    }

    private getDefaultMenuMode(): MenuMode {
        return this.isDesktop() ? 'static' : 'slim';
    }

    private applyResponsiveMenuMode(): void {
        const nextMode = this.getDefaultMenuMode();
        if (this.config().menuMode === nextMode) {
            return;
        }

        this.config.update((cfg) => ({
            ...cfg,
            menuMode: nextMode,
        }));

        this.state.staticMenuDesktopInactive = false;
        this.state.staticMenuMobileActive = false;
        this.state.overlayMenuActive = false;
        this.state.menuHoverActive = false;
    }

    private loadConfig(): Partial<AppConfig> | null {
        try {
            const raw = localStorage.getItem(this.storageKey);
            if (!raw) {
                return null;
            }
            const parsed = JSON.parse(raw);
            if (!parsed || typeof parsed !== 'object') {
                return null;
            }
            return this.sanitizeConfig(parsed);
        } catch {
            return null;
        }
    }

    private saveConfig(config: AppConfig): void {
        try {
            localStorage.setItem(this.storageKey, JSON.stringify(config));
        } catch {
            // Ignore quota / private-mode failures.
        }
    }

    private sanitizeConfig(value: Record<string, unknown>): Partial<AppConfig> {
        const menuModes: MenuMode[] = ['static', 'overlay', 'slim', 'slim-plus'];
        const result: Partial<AppConfig> = {};

        const theme = value['theme'];
        if (typeof theme === 'string' && theme.trim()) {
            result.theme = theme;
        }
        const colorScheme = value['colorScheme'];
        if (colorScheme === 'light' || colorScheme === 'dark') {
            result.colorScheme = colorScheme;
        }
        const menuMode = value['menuMode'];
        if (
            typeof menuMode === 'string' &&
            menuModes.includes(menuMode as MenuMode)
        ) {
            result.menuMode = menuMode as MenuMode;
        }
        const layoutTheme = value['layoutTheme'];
        if (typeof layoutTheme === 'string' && layoutTheme.trim()) {
            result.layoutTheme = layoutTheme;
        }
        const inputStyle = value['inputStyle'];
        if (inputStyle === 'outlined' || inputStyle === 'filled') {
            result.inputStyle = inputStyle;
        }
        const ripple = value['ripple'];
        if (typeof ripple === 'boolean') {
            result.ripple = ripple;
        }
        const scale = value['scale'];
        if (typeof scale === 'number' && scale >= 12 && scale <= 16) {
            result.scale = scale;
        }

        return result;
    }

    updateStyle(config: AppConfig) {
        return (
            config.theme !== this._config.theme ||
            config.colorScheme !== this._config.colorScheme
        );
    }

    changeTheme() {
        const config = this.config();
        const themeLink = <HTMLLinkElement>(
            document.getElementById('theme-link')
        );
        const themeLinkHref = themeLink.getAttribute('href')!;
        const newHref = themeLinkHref
            .split('/')
            .map((el) =>
                el == this._config.theme
                    ? (el = config.theme)
                    : el == `theme-${this._config.colorScheme}`
                    ? (el = `theme-${config.colorScheme}`)
                    : el
            )
            .join('/');

        this.replaceThemeLink(newHref);
    }

    replaceThemeLink(href: string) {
        const id = 'theme-link';
        let themeLink = <HTMLLinkElement>document.getElementById(id);
        const cloneLinkElement = <HTMLLinkElement>themeLink.cloneNode(true);

        cloneLinkElement.setAttribute('href', href);
        cloneLinkElement.setAttribute('id', id + '-clone');

        themeLink.parentNode!.insertBefore(
            cloneLinkElement,
            themeLink.nextSibling
        );
        cloneLinkElement.addEventListener('load', () => {
            themeLink.remove();
            cloneLinkElement.setAttribute('id', id);
        });
    }

    onMenuToggle() {
        if (this.isOverlay()) {
            this.state.overlayMenuActive = !this.state.overlayMenuActive;

            if (this.state.overlayMenuActive) {
                this.overlayOpen.next(null);
            }
        }
        if (this.isDesktop()) {
            this.state.staticMenuDesktopInactive =
                !this.state.staticMenuDesktopInactive;
        } else {
            this.state.staticMenuMobileActive =
                !this.state.staticMenuMobileActive;

            if (this.state.staticMenuMobileActive) {
                this.overlayOpen.next(null);
            }
        }
    }

    onOverlaySubmenuOpen() {
        this.overlayOpen.next(null);
    }

    showProfileSidebar() {
        this.state.profileSidebarVisible = true;
    }

    showConfigSidebar() {
        this.state.configSidebarVisible = true;
    }

    isDesktop() {
        return window.innerWidth > 991;
    }

    isOverlay() {
        return this.config().menuMode === 'overlay';
    }

    isSlim() {
        return this.config().menuMode === 'slim';
    }

    isSlimPlus() {
        return this.config().menuMode === 'slim-plus';
    }

    isMobile() {
        return !this.isDesktop();
    }

    onConfigUpdate() {
        this._config = { ...this.config() };
        this.configUpdate.next(this.config());
        this.saveConfig(this._config);
    }

    onTabOpen(value: MenuItem) {
        this.tabOpen.next(value);
    }

    openTab(value: MenuItem) {
        this.tabs = [...this.tabs, value];
    }

    onTabClose(value: MenuItem, index: number) {
        this.tabClose.next({ tab: value, index: index });
    }

    closeTab(index: number) {
        this.tabs.splice(index, 1);
        this.tabs = [...this.tabs];
    }

    changeScale(value: number) {
        document.documentElement.style.fontSize = `${value}px`;
    }
}
