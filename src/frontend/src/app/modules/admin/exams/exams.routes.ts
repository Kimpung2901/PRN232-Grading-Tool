import { Routes } from '@angular/router';

const routes: Routes = [
    {
        path: 'data',
        loadComponent: () =>
            import('./data/data.component').then(
                (m) => m.DataComponent
            ),
        data: { title: 'Exams' },
    },
    {
        path: 'data/:id',
        loadComponent: () =>
            import('./detail/detail.component').then(
                (m) => m.DetailComponent
            ),
        data: { title: 'Exam Details' },
    },
    {
        path: 'data/:id/test-cases',
        loadComponent: () =>
            import('./test-cases/test-cases.component').then(
                (m) => m.TestCasesComponent
            ),
        data: { title: 'Test Case Configuration' },
    },
    {
        path: 'data/create',
        loadComponent: () =>
            import('./create/create.component').then(
                (m) => m.CreateComponent
            ),
        data: { title: 'Create exam' },
    },
    {
        path: 'data/runner',
        loadComponent: () =>
            import('./runner/runner.component').then(
                (m) => m.RunnerComponent
            ),
        data: { title: 'Grading Runner' },
    },
    { path: 'create', redirectTo: 'data/create' },
    { path: 'runner', redirectTo: 'data/runner' },
    { path: '', pathMatch: 'full', redirectTo: 'data' },
];

export default routes;
