import { Routes } from '@angular/router';

const routes: Routes = [
    {
        path: 'dashboard',
        loadComponent: () =>
            import('./dashboard/dashboard.component').then(
                (m) => m.DashboardComponent
            ),
        data: { title: 'Exams dashboard' },
    },
    {
        path: 'data',
        loadComponent: () =>
            import('./data/data.component').then(
                (m) => m.DataComponent
            ),
        data: { title: 'Exams Data Manager' },
    },
    {
        path: 'create',
        loadComponent: () =>
            import('./create/create.component').then(
                (m) => m.CreateComponent
            ),
        data: { title: 'Create exam' },
    },
    {
        path: ':id',
        loadComponent: () =>
            import('./detail/detail.component').then(
                (m) => m.DetailComponent
            ),
        data: { title: 'Exam Details' },
    },
    {
        path: ':id/test-cases',
        loadComponent: () =>
            import('./test-cases/test-cases.component').then(
                (m) => m.TestCasesComponent
            ),
        data: { title: 'Test case configuration' },
    },
    { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
];

export default routes;

