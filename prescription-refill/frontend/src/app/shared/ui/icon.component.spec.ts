import { TestBed } from '@angular/core/testing';
import { IconComponent } from './icon.component';

describe('IconComponent', () => {
  function render(name: string) {
    const fixture = TestBed.createComponent(IconComponent);
    fixture.componentRef.setInput('name', name);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  it('renders a decorative svg for a known icon', () => {
    const el = render('alert-octagon');

    expect(el.getAttribute('aria-hidden')).toBe('true');
    expect(el.querySelector('svg')).not.toBeNull();
    expect(el.querySelectorAll('path').length).toBeGreaterThan(0);
  });

  it('renders nothing for an unknown icon', () => {
    expect(render('no-such-icon').querySelector('svg')).toBeNull();
  });
});
