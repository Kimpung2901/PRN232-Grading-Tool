import { Routes } from '@angular/router';

const routes: Routes = [
    {
        path: 'dashboard',
        loadComponent: () =>
            import('./dashboard/dashboard.component').then(
                (m) => m.DashboardComponent
            ),
        data: { title: 'Submission dashboard' },
    },
    {
        path: 'data',
        loadComponent: () =>
            import('./data/data.component').then(
                (m) => m.DataComponent
            ),
        data: { title: 'Submissions Data' },
    },
    {
        path: 'create',
        loadComponent: () =>
            import('./create/create.component').then(
                (m) => m.CreateComponent
            ),
        data: { title: 'Create submission' },
    },
    {
        path: ':id',
        loadComponent: () =>
            import('./detail/detail.component').then(
                (m) => m.DetailComponent
            ),
        data: { title: 'Submission Details' },
    },
    {
        path: ':id/grading',
        loadComponent: () =>
            import('./grading/grading.component').then(
                (m) => m.GradingComponent
            ),
        data: { title: 'Submission grading' },
    },
    { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
];

export default routes;

