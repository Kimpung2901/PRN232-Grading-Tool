import { Routes } from '@angular/router';

const routes: Routes = [
    {
        path: 'data',
        loadComponent: () =>
            import('./data/data.component').then(
                (m) => m.DataComponent
            ),
        data: { title: 'Submissions' },
    },
    {
        path: 'data/create',
        loadComponent: () =>
            import('./create/create.component').then(
                (m) => m.CreateComponent
            ),
        data: { title: 'Create submission' },
    },
    {
        path: 'data/:id',
        loadComponent: () =>
            import('./detail/detail.component').then(
                (m) => m.DetailComponent
            ),
        data: { title: 'Submission Details' },
    },
    {
        path: 'data/:id/grading',
        loadComponent: () =>
            import('./grading/grading.component').then(
                (m) => m.GradingComponent
            ),
        data: { title: 'Submission Grading Report' },
    },
    { path: 'create', redirectTo: 'data/create' },
    { path: '', pathMatch: 'full', redirectTo: 'data' },
];

export default routes;
