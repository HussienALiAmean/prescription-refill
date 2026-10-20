import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

/*
 * Icon outlines adapted from Lucide (https://lucide.dev), ISC License,
 * Copyright (c) for portions of Lucide are held by Cole Bemis 2013-2022 as part of Feather (MIT);
 * all other copyright (c) for Lucide are held by Lucide Contributors 2022.
 * Circles and rects are expressed as paths so every icon is a list of `d` strings.
 */
const ICONS = {
  'alert-octagon': [
    'M12 16h.01',
    'M12 8v4',
    'M15.312 2a2 2 0 0 1 1.414.586l4.688 4.688A2 2 0 0 1 22 8.688v6.624a2 2 0 0 1-.586 1.414l-4.688 4.688a2 2 0 0 1-1.414.586H8.688a2 2 0 0 1-1.414-.586l-4.688-4.688A2 2 0 0 1 2 15.312V8.688a2 2 0 0 1 .586-1.414l4.688-4.688A2 2 0 0 1 8.688 2z',
  ],
  'alert-triangle': ['m21.73 18-8-14a2 2 0 0 0-3.48 0l-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.73-3', 'M12 9v4', 'M12 17h.01'],
  'arrow-left': ['m12 19-7-7 7-7', 'M19 12H5'],
  calendar: ['M8 2v4', 'M16 2v4', 'M5 4h14a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2z', 'M3 10h18'],
  'check-circle': ['M2 12a10 10 0 1 0 20 0a10 10 0 1 0-20 0', 'm9 12 2 2 4-4'],
  'chevron-left': ['m15 18-6-6 6-6'],
  'chevron-right': ['m9 18 6-6-6-6'],
  clock: ['M2 12a10 10 0 1 0 20 0a10 10 0 1 0-20 0', 'M12 6v6l4 2'],
  inbox: [
    'M22 12h-6l-2 3h-4l-2-3H2',
    'M5.45 5.11 2 12v6a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2v-6l-3.45-6.89A2 2 0 0 0 16.76 4H7.24a2 2 0 0 0-1.79 1.11z',
  ],
  info: ['M2 12a10 10 0 1 0 20 0a10 10 0 1 0-20 0', 'M12 16v-4', 'M12 8h.01'],
  lock: ['M5 11h14a2 2 0 0 1 2 2v7a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-7a2 2 0 0 1 2-2z', 'M7 11V7a5 5 0 0 1 10 0v4'],
  pill: ['m10.5 20.5 10-10a4.95 4.95 0 1 0-7-7l-10 10a4.95 4.95 0 1 0 7 7Z', 'm8.5 8.5 7 7'],
  refresh: [
    'M3 12a9 9 0 0 1 9-9 9.75 9.75 0 0 1 6.74 2.74L21 8',
    'M21 3v5h-5',
    'M21 12a9 9 0 0 1-9 9 9.75 9.75 0 0 1-6.74-2.74L3 16',
    'M8 16H3v5',
  ],
  search: ['M3 11a8 8 0 1 0 16 0a8 8 0 1 0-16 0', 'm21 21-4.3-4.3'],
  'search-x': ['m13.5 8.5-5 5', 'm8.5 8.5 5 5', 'M3 11a8 8 0 1 0 16 0a8 8 0 1 0-16 0', 'm21 21-4.3-4.3'],
  'shield-check': [
    'M20 13c0 5-3.5 7.5-7.66 8.95a1 1 0 0 1-.67-.01C7.5 20.5 4 18 4 13V6a1 1 0 0 1 1-1c2 0 4.5-1.2 6.24-2.72a1.17 1.17 0 0 1 1.52 0C14.51 3.81 17 5 19 5a1 1 0 0 1 1 1z',
    'm9 12 2 2 4-4',
  ],
  user: ['M19 21v-2a4 4 0 0 0-4-4H9a4 4 0 0 0-4 4v2', 'M8 7a4 4 0 1 0 8 0a4 4 0 1 0-8 0'],
  'wifi-off': [
    'M12 20h.01',
    'M8.5 16.429a5 5 0 0 1 7 0',
    'M5 12.859a10 10 0 0 1 5.17-2.69',
    'M19 12.859a10 10 0 0 0-2.007-1.523',
    'M2 8.82a15 15 0 0 1 4.177-2.643',
    'M22 8.82a15 15 0 0 0-11.288-3.764',
    'm2 2 20 20',
  ],
  x: ['M18 6 6 18', 'm6 6 12 12'],
  'x-circle': ['M2 12a10 10 0 1 0 20 0a10 10 0 1 0-20 0', 'm15 9-6 6', 'm9 9 6 6'],
} satisfies Record<string, string[]>;

export type IconName = keyof typeof ICONS;

/**
 * Decorative outline icon (research R3). Always hidden from assistive technology: meaning is
 * carried by adjacent text, never by the icon alone.
 */
@Component({
  selector: 'app-icon',
  template: `
    @if (paths().length > 0) {
      <svg
        xmlns="http://www.w3.org/2000/svg"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        stroke-width="2"
        stroke-linecap="round"
        stroke-linejoin="round"
        [attr.width]="size()"
        [attr.height]="size()"
        focusable="false"
      >
        @for (d of paths(); track $index) {
          <path [attr.d]="d" />
        }
      </svg>
    }
  `,
  styles: `
    :host {
      display: inline-flex;
      flex: none;
      line-height: 0;
    }
  `,
  host: { 'aria-hidden': 'true' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class IconComponent {
  readonly name = input.required<IconName | string>();
  readonly size = input(18);

  protected readonly paths = computed<readonly string[]>(() => ICONS[this.name() as IconName] ?? []);
}
