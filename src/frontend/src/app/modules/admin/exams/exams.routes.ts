import { Routes } from '@angular/router';

const routes: Routes = [
    {
        path: 'data',
        data: { title: 'Exams' },
        children: [
            {
                path: '',
                loadComponent: () =>
                    import('./data/data.component').then(
                        (m) => m.DataComponent
                    ),
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
        ]
    },
    {
        path: 'create',
        loadComponent: () =>
            import('./create/create.component').then(
                (m) => m.CreateComponent
            ),
        data: { title: 'Create exam' },
    },
    { path: '', pathMatch: 'full', redirectTo: 'data' },
];

export default routes;
