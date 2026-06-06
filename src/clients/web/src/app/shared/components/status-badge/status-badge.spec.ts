import { TestBed } from '@angular/core/testing';
import { StatusBadge } from './status-badge';

describe('StatusBadge', () => {
  beforeEach(() => TestBed.configureTestingModule({ imports: [StatusBadge] }));

  it('humanises a PascalCase status into a spaced label', () => {
    const fixture = TestBed.createComponent(StatusBadge);
    fixture.componentRef.setInput('status', 'UnderInvestigation');
    fixture.detectChanges();

    expect(fixture.componentInstance.label()).toBe('Under Investigation');
  });

  it('renders the raw status as data-status (colour hook) and the label as text', () => {
    const fixture = TestBed.createComponent(StatusBadge);
    fixture.componentRef.setInput('status', 'Open');
    fixture.detectChanges();

    const badge = (fixture.nativeElement as HTMLElement).querySelector('.badge');
    expect(badge?.getAttribute('data-status')).toBe('Open');
    expect(badge?.textContent?.trim()).toBe('Open');
  });
});
