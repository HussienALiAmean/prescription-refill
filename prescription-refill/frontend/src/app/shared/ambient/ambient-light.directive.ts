import { DestroyRef, Directive, ElementRef, NgZone, inject } from '@angular/core';

/**
 * Shared ambient pointer light (004 spec FR-017–FR-023; research R4). Placed once on the app
 * shell; never adds listeners to individual elements.
 *
 * Writes two coordinate sets, each in the space of the layer that uses it:
 * - `--pointer-x/--pointer-y` on the host: viewport coordinates (`clientX/clientY`), because the
 *   canvas light is `position: fixed`; so the light stays under the pointer at any scroll position.
 * - `--glow-x/--glow-y` on the `[data-glow]` element under the pointer: coordinates relative to
 *   that element, because its glow is drawn inside it.
 *
 * Active only for hover-capable fine pointers without reduced motion (switching live). One passive
 * listener runs outside Angular's zone, so pointer movement never triggers change detection, and at
 * most one animation frame is scheduled per movement burst; nothing runs while the pointer is
 * still. Listeners, media listeners, and any pending frame are released when the host is destroyed.
 */
@Directive({ selector: '[appAmbientLight]' })
export class AmbientLightDirective {
  private readonly host: HTMLElement = inject(ElementRef).nativeElement;
  private readonly zone = inject(NgZone);

  private active = false;
  private frame: number | null = null;
  private lastEvent: PointerEvent | null = null;
  private glowing: HTMLElement | null = null;

  constructor() {
    const finePointer = matchMedia('(hover: hover) and (pointer: fine)');
    const reducedMotion = matchMedia('(prefers-reduced-motion: reduce)');
    const update = () => this.setActive(finePointer.matches && !reducedMotion.matches);

    finePointer.addEventListener('change', update);
    reducedMotion.addEventListener('change', update);
    update();

    inject(DestroyRef).onDestroy(() => {
      finePointer.removeEventListener('change', update);
      reducedMotion.removeEventListener('change', update);
      this.setActive(false);
    });
  }

  private readonly onMove = (event: PointerEvent): void => {
    if (event.pointerType === 'touch') {
      return;
    }
    this.lastEvent = event;
    this.frame ??= requestAnimationFrame(this.render);
  };

  private readonly onLeave = (): void => this.clear();

  private readonly render = (): void => {
    this.frame = null;
    const event = this.lastEvent;
    if (!event) {
      return;
    }

    this.host.style.setProperty('--pointer-x', `${event.clientX}px`);
    this.host.style.setProperty('--pointer-y', `${event.clientY}px`);

    const target = event.target instanceof Element ? event.target.closest<HTMLElement>('[data-glow]') : null;
    const glow = target && this.host.contains(target) ? target : null;
    if (this.glowing && this.glowing !== glow) {
      this.clearGlow();
    }
    if (glow) {
      const rect = glow.getBoundingClientRect();
      glow.style.setProperty('--glow-x', `${event.clientX - rect.left}px`);
      glow.style.setProperty('--glow-y', `${event.clientY - rect.top}px`);
      this.glowing = glow;
    }
  };

  private setActive(active: boolean): void {
    if (active === this.active) {
      return;
    }
    this.active = active;

    if (active) {
      this.host.setAttribute('data-ambient', 'on');
      this.zone.runOutsideAngular(() => {
        this.host.addEventListener('pointermove', this.onMove, { passive: true });
        this.host.addEventListener('pointerleave', this.onLeave, { passive: true });
      });
    } else {
      this.host.removeAttribute('data-ambient');
      this.host.removeEventListener('pointermove', this.onMove);
      this.host.removeEventListener('pointerleave', this.onLeave);
      this.clear();
    }
  }

  /** Cancels a pending frame and returns the light and glow to their resting positions. */
  private clear(): void {
    if (this.frame !== null) {
      cancelAnimationFrame(this.frame);
      this.frame = null;
    }
    this.lastEvent = null;
    this.host.style.removeProperty('--pointer-x');
    this.host.style.removeProperty('--pointer-y');
    this.clearGlow();
  }

  private clearGlow(): void {
    this.glowing?.style.removeProperty('--glow-x');
    this.glowing?.style.removeProperty('--glow-y');
    this.glowing = null;
  }
}
