import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { acceptLanguageInterceptor } from './accept-language.interceptor';
import { I18nService } from './i18n.service';
import { provideI18nTesting } from './testing';

describe('acceptLanguageInterceptor', () => {
  let http: HttpClient;
  let backend: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        ...provideI18nTesting('ar'),
        provideHttpClient(withInterceptors([acceptLanguageInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpClient);
    backend = TestBed.inject(HttpTestingController);
  });

  afterEach(() => backend.verify());

  it('states the active language on API calls (spec 005 FR-010)', () => {
    http.get('/api/refill-requests').subscribe();
    expect(backend.expectOne('/api/refill-requests').request.headers.get('Accept-Language')).toBe('ar');

    TestBed.inject(I18nService).setLanguage('en');
    http.get('/api/refill-requests/5').subscribe();
    expect(backend.expectOne('/api/refill-requests/5').request.headers.get('Accept-Language')).toBe('en');
  });

  it('leaves other requests alone', () => {
    http.get('/assets/logo.svg').subscribe();

    expect(backend.expectOne('/assets/logo.svg').request.headers.has('Accept-Language')).toBeFalse();
  });
});
