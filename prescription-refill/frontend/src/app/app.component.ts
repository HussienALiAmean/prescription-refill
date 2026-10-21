import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { I18nService } from './core/i18n/i18n.service';
import { Language } from './core/i18n/language';
import { Theme } from './core/preferences/preferences.store';
import { ThemeService } from './core/theme/theme.service';
import { ConnectionIndicatorComponent } from './shared/ui/connection-indicator/connection-indicator.component';
import { AmbientLightDirective } from './shared/ambient/ambient-light.directive';
import { IconComponent } from './shared/ui/icon.component';
import { PreferenceOption, PreferenceSwitchComponent } from './shared/ui/preference-switch.component';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, ConnectionIndicatorComponent, IconComponent, PreferenceSwitchComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  // One shared pointer light for the whole app (004 research R4).
  hostDirectives: [AmbientLightDirective],
})
export class AppComponent {
  protected readonly i18n = inject(I18nService);
  protected readonly themes = inject(ThemeService);
  protected readonly t = this.i18n.t;

  /** Each language is named in its own language (spec 005 FR-008). */
  protected readonly languageOptions: readonly PreferenceOption<Language>[] = [
    { value: 'ar', label: 'العربية', lang: 'ar' },
    { value: 'en', label: 'English', lang: 'en' },
  ];

  protected readonly themeOptions = computed<readonly PreferenceOption<Theme>[]>(() => [
    { value: 'light', label: this.t().app.themeLight, icon: 'sun' },
    { value: 'dark', label: this.t().app.themeDark, icon: 'moon' },
  ]);

  /** Polite announcement of the last preference change, in the newly active language (FR-035). */
  protected readonly announcement = signal('');

  protected setLanguage(language: Language): void {
    this.i18n.setLanguage(language);
    this.announcement.set(this.t().app.languageChanged);
  }

  protected setTheme(theme: Theme): void {
    this.themes.setTheme(theme);
    this.announcement.set(this.t().app.themeChanged[theme]);
  }
}
