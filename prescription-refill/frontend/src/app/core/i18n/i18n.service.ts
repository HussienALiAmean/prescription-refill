import { DOCUMENT } from '@angular/common';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, Subject } from 'rxjs';
import { PreferencesStore } from '../preferences/preferences.store';
import { ar } from './ar';
import { en } from './en';
import { Language, directionOf } from './language';
import { Messages } from './messages';

const DICTIONARIES: Record<Language, Messages> = { ar, en };

/**
 * The active interface language (spec 005 FR-007–FR-013, research R9). Components read
 * `t()` for text; the service owns `html[lang]`, `html[dir]`, and `document.title`. Switching
 * needs no reload: `languageChanged$` asks pages to reload their server data.
 */
@Injectable({ providedIn: 'root' })
export class I18nService {
  private readonly store = inject(PreferencesStore);
  private readonly document = inject(DOCUMENT);
  private readonly changes = new Subject<Language>();

  readonly language = signal<Language>(this.store.language());
  readonly direction = computed(() => directionOf(this.language()));
  readonly t = computed(() => DICTIONARIES[this.language()]);

  /** Emits after a user change only, never for the initial language. */
  readonly languageChanged$: Observable<Language> = this.changes.asObservable();

  constructor() {
    this.apply();
  }

  setLanguage(language: Language): void {
    if (language === this.language()) {
      return;
    }

    this.store.setLanguage(language);
    this.language.set(language);
    this.apply();
    this.changes.next(language);
  }

  private apply(): void {
    const root = this.document.documentElement;
    root.lang = this.language();
    root.dir = this.direction();
    this.document.title = this.t().app.documentTitle;
  }
}
