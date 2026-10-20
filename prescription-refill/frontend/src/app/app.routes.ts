import { Routes } from '@angular/router';
import { RefillQueuePage } from './features/refill-requests/queue/refill-queue.page';
import { RefillReviewPage } from './features/refill-requests/review/refill-review.page';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'requests' },
  { path: 'requests', component: RefillQueuePage, title: 'Refill requests' },
  { path: 'requests/:id', component: RefillReviewPage, title: 'Review refill request' },
  { path: '**', redirectTo: 'requests' },
];
