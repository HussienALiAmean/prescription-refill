import { Component, NgZone } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AmbientLightDirective } from './ambient-light.directive';
import { provideI18nTesting } from '../../core/i18n/testing';

@Component({
  imports: [AmbientLightDirective],
  template: `
    <div appAmbientLight id="host" style="position: relative; width: 400px; height: 300px">
      <ul style="margin: 0; padding: 0">
        <li data-glow id="row" style="display: block; width: 200px; height: 40px; margin: 50px 0 0 30px">row</li>
      </ul>
      <span id="plain">plain</span>
    </div>
  `,
})
class HostComponent {}

type MediaListener = (event: { matches: boolean }) => void;

interface FakeMediaList {
  media: string;
  matches: boolean;
  listeners: MediaListener[];
  addEventListener(type: string, listener: MediaListener): void;
  removeEventListener(type: string, listener: MediaListener): void;
}

describe('AmbientLightDirective', () => {
  /** What the fake media queries report: hover-capable fine pointer, reduced motion. */
  let mediaOptions: { fine: boolean; reduced: boolean };
  let media: FakeMediaList[];
  let frames: Map<number, FrameRequestCallback>;
  let nextFrame: number;
  let scheduledInAngularZone: boolean[];
  let fixture: ComponentFixture<HostComponent>;
  let host: HTMLElement;
  let row: HTMLElement;
  let plain: HTMLElement;

  function render() {
    fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    host = el.querySelector('#host')!;
    row = el.querySelector('#row')!;
    plain = el.querySelector('#plain')!;
  }

  function setMedia(fragment: string, matches: boolean) {
    const list = media.find((l) => l.media.includes(fragment))!;
    list.matches = matches;
    list.listeners.forEach((listener) => listener({ matches }));
  }

  function move(target: Element, clientX: number, clientY: number, pointerType = 'mouse') {
    target.dispatchEvent(new PointerEvent('pointermove', { clientX, clientY, bubbles: true, pointerType }));
  }

  function flushFrames() {
    const pending = [...frames.values()];
    frames.clear();
    pending.forEach((callback) => callback(performance.now()));
  }

  const vars = (el: HTMLElement, x: string, y: string) => [el.style.getPropertyValue(x), el.style.getPropertyValue(y)];

  beforeEach(() => {
    mediaOptions = { fine: true, reduced: false };
    media = [];
    spyOn(window, 'matchMedia').and.callFake((query: string) => {
      const list: FakeMediaList = {
        media: query,
        matches: query.includes('prefers-reduced-motion') ? mediaOptions.reduced : mediaOptions.fine,
        listeners: [],
        addEventListener: (_type, listener) => list.listeners.push(listener),
        removeEventListener: (_type, listener) => (list.listeners = list.listeners.filter((l) => l !== listener)),
      };
      media.push(list);
      return list as unknown as MediaQueryList;
    });

    frames = new Map();
    nextFrame = 0;
    scheduledInAngularZone = [];
    spyOn(window, 'requestAnimationFrame').and.callFake((callback: FrameRequestCallback) => {
      scheduledInAngularZone.push(NgZone.isInAngularZone());
      frames.set(++nextFrame, callback);
      return nextFrame;
    });
    spyOn(window, 'cancelAnimationFrame').and.callFake((id: number) => frames.delete(id));

    TestBed.configureTestingModule({ providers: provideI18nTesting(), imports: [HostComponent] });
  });

  it('enables ambient mode only for fine, hover-capable pointers without reduced motion', () => {
    const cases = [
      { fine: true, reduced: false, expected: true },
      { fine: false, reduced: false, expected: false },
      { fine: true, reduced: true, expected: false },
    ];
    for (const { fine, reduced, expected } of cases) {
      mediaOptions = { fine, reduced };
      render();
      expect(host.getAttribute('data-ambient') === 'on').withContext(JSON.stringify({ fine, reduced })).toBe(expected);
      fixture.destroy();
    }
  });

  it('switches off when reduced motion is turned on later, and back on when it is turned off', () => {
    render();
    move(plain, 120, 80);
    flushFrames();
    expect(vars(host, '--pointer-x', '--pointer-y')).toEqual(['120px', '80px']);

    setMedia('prefers-reduced-motion', true);
    expect(host.hasAttribute('data-ambient')).toBeFalse();
    expect(vars(host, '--pointer-x', '--pointer-y')).toEqual(['', '']);
    move(plain, 10, 10);
    expect(frames.size).toBe(0);

    setMedia('prefers-reduced-motion', false);
    expect(host.getAttribute('data-ambient')).toBe('on');
  });

  it('writes viewport pointer coordinates once per frame', () => {
    render();

    move(plain, 100, 60);
    move(plain, 140, 90);
    expect(frames.size).toBe(1);

    flushFrames();
    expect(vars(host, '--pointer-x', '--pointer-y')).toEqual(['140px', '90px']);
  });

  it('keeps pointer coordinates in viewport space when the page is scrolled', () => {
    render();
    // The host has moved up by 500px, as when the page is scrolled down.
    host.style.transform = 'translateY(-500px)';
    expect(host.getBoundingClientRect().top).toBeLessThan(0);

    move(plain, 220, 120);
    flushFrames();

    expect(vars(host, '--pointer-x', '--pointer-y')).toEqual(['220px', '120px']);
  });

  it('writes glow coordinates relative to the hovered data-glow element only', () => {
    render();
    const rect = row.getBoundingClientRect();

    move(row, rect.left + 25, rect.top + 15);
    flushFrames();

    expect(vars(row, '--glow-x', '--glow-y')).toEqual(['25px', '15px']);
    expect(vars(host, '--glow-x', '--glow-y')).toEqual(['', '']);
    expect(vars(plain, '--glow-x', '--glow-y')).toEqual(['', '']);
  });

  it('schedules no frame without movement', () => {
    render();
    move(plain, 50, 50);
    flushFrames();

    expect(frames.size).toBe(0);
    expect(window.requestAnimationFrame).toHaveBeenCalledTimes(1);
  });

  it("handles pointer moves outside Angular's zone, so they never trigger change detection", () => {
    render();

    move(plain, 30, 30);

    expect(scheduledInAngularZone).toEqual([false]);
  });

  it('clears its coordinates when the pointer leaves the host', () => {
    render();
    const rect = row.getBoundingClientRect();
    move(row, rect.left + 5, rect.top + 5);
    flushFrames();

    host.dispatchEvent(new PointerEvent('pointerleave', { pointerType: 'mouse' }));

    expect(vars(host, '--pointer-x', '--pointer-y')).toEqual(['', '']);
    expect(vars(row, '--glow-x', '--glow-y')).toEqual(['', '']);
  });

  it('ignores touch pointers', () => {
    render();

    move(plain, 5, 5, 'touch');

    expect(frames.size).toBe(0);
  });

  it('adds no listeners and writes nothing when disabled', () => {
    mediaOptions = { fine: false, reduced: false };
    render();

    move(plain, 40, 40);

    expect(frames.size).toBe(0);
    expect(vars(host, '--pointer-x', '--pointer-y')).toEqual(['', '']);
  });

  it('removes listeners and cancels a pending frame on destroy', () => {
    render();
    move(plain, 40, 40);
    expect(frames.size).toBe(1);

    fixture.destroy();

    expect(frames.size).toBe(0);
    expect(media.every((list) => list.listeners.length === 0)).toBeTrue();
    move(plain, 60, 60);
    expect(frames.size).toBe(0);
  });
});
