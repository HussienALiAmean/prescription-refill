import { Injectable, inject } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { RouterStateSnapshot, TitleStrategy } from '@angular/router';
import { I18nService } from './i18n.service';
import { Messages } from './messages';

export type PageTitle = keyof Messages['app']['pageTitles'];

/**
 * Route titles are dictionary keys (`title: 'queue'`), shown in the active language and updated
 * on a language switch (spec 005 FR-017: page titles in both languages).
 */
@Injectable({ providedIn: 'root' })
export class LocalizedTitleStrategy extends TitleStrategy {
  private readonly i18n = inject(I18nService);
  private readonly title = inject(Title);
  private current: PageTitle | undefined;

  constructor() {
    super();
    this.i18n.languageChanged$.subscribe(() => this.apply());
  }

  override updateTitle(snapshot: RouterStateSnapshot): void {
    this.current = this.buildTitle(snapshot) as PageTitle | undefined;
    this.apply();
  }

  private apply(): void {
    const t = this.i18n.t().app;
    this.title.setTitle(this.current ? t.pageTitles[this.current] : t.documentTitle);
  }
}
