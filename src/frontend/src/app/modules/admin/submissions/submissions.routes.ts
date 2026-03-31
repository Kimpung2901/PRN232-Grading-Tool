import { Routes } from '@angular/router';

const routes: Routes = [
    {
        path: 'data',
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
        ]
    },
    {
        path: 'create',
        loadComponent: () =>
            import('./create/create.component').then(
                (m) => m.CreateComponent
            ),
        data: { title: 'Create submission' },
    },
    { path: '', pathMatch: 'full', redirectTo: 'data' },
];

export default routes;
