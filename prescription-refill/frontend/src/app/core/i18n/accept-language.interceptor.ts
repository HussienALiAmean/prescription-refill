import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { I18nService } from './i18n.service';

/**
 * States the active language on every API call (spec 005 FR-010, research R10), so the browser's
 * own language never decides. HTTP calls themselves stay in the feature API services.
 */
export const acceptLanguageInterceptor: HttpInterceptorFn = (request, next) => {
  if (!new URL(request.url, 'http://local').pathname.startsWith('/api/')) {
    return next(request);
  }

  const language = inject(I18nService).language();
  return next(request.clone({ setHeaders: { 'Accept-Language': language } }));
};
