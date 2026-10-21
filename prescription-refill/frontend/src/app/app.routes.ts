import { Routes } from '@angular/router';
import { PageTitle } from './core/i18n/localized-title.strategy';
import { RefillQueuePage } from './features/refill-requests/queue/refill-queue.page';
import { RefillReviewPage } from './features/refill-requests/review/refill-review.page';

// Titles are dictionary keys; LocalizedTitleStrategy shows them in the active language (spec 005 FR-017).
const title = (key: PageTitle) => key;

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'requests' },
  { path: 'requests', component: RefillQueuePage, title: title('queue') },
  { path: 'requests/:id', component: RefillReviewPage, title: title('review') },
  { path: '**', redirectTo: 'requests' },
];
