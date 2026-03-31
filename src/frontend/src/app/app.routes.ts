import { Route } from '@angular/router';
import { initialDataResolver } from 'app/app.resolvers';
import { LayoutComponent } from 'app/layout/layout.component';

// @formatter:off
/* eslint-disable max-len */
/* eslint-disable @typescript-eslint/explicit-function-return-type */
export const appRoutes: Route[] = [
    { path: '', pathMatch: 'full', redirectTo: 'dashboard' },

    { path: 'signed-in-redirect', pathMatch: 'full', redirectTo: 'dashboard' },

    {
        path: '',
        component: LayoutComponent,
        resolve: { initialData: initialDataResolver },
        children: [
            {
                path: 'dashboard',
                loadChildren: () =>
                    import('app/modules/admin/dashboard/dashboard.routes'),
            },
            {
                path: 'exams',
                loadChildren: () =>
                    import('app/modules/admin/exams/exams.routes'),
            },
            {
                path: 'submissions',
                loadChildren: () =>
                    import('app/modules/admin/submissions/submissions.routes'),
            },
        ],
    },
];
// @formatter:on
