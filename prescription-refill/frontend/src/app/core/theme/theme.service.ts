import { DOCUMENT } from '@angular/common';
import { Injectable, inject, signal } from '@angular/core';
import { PreferencesStore, Theme } from '../preferences/preferences.store';

/**
 * The active theme (spec 005 FR-026–FR-028). It owns `html[data-theme]` and the color-scheme
 * meta, writes only the theme key, and needs no data reload.
 */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly store = inject(PreferencesStore);
  private readonly document = inject(DOCUMENT);

  readonly theme = signal<Theme>(this.store.theme());

  constructor() {
    this.apply();
  }

  setTheme(theme: Theme): void {
    if (theme === this.theme()) {
      return;
    }

    this.store.setTheme(theme);
    this.theme.set(theme);
    this.apply();
  }

  private apply(): void {
    const theme = this.theme();
    this.document.documentElement.dataset['theme'] = theme;
    this.document.querySelector('meta[name="color-scheme"]')?.setAttribute('content', theme);
  }
}
