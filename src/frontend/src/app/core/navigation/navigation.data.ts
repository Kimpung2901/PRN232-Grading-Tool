/* eslint-disable */
import { FuseNavigationItem } from '@fuse/components/navigation';

export const defaultNavigation: FuseNavigationItem[] = [
    {
        id: 'dashboard',
        title: 'Dashboard',
        type: 'aside',
        icon: 'heroicons_outline:squares-2x2',
        children: [
            {
                id: 'dashboard.index',
                title: 'Dashboard',
                type: 'basic',
                icon: 'heroicons_outline:squares-2x2',
                link: '/dashboard',
            },
        ],
    },
    {
        id: 'exams',
        title: 'Exams',
        type: 'aside',
        icon: 'heroicons_outline:academic-cap',
        children: [
            {
                id: 'exams.list',
                title: 'Exams',
                type: 'basic',
                icon: 'heroicons_outline:academic-cap',
                link: '/exams/data',
                exactMatch: true
            },
            {
                id: 'exams.create',
                title: 'Create Exam',
                type: 'basic',
                icon: 'heroicons_outline:plus-circle',
                link: '/exams/data/create',
                exactMatch: true
            },
            {
                id: 'exams.runner',
                title: 'Grading Runner',
                type: 'basic',
                icon: 'heroicons_outline:command-line',
                link: '/exams/data/runner',
                exactMatch: true
            },
        ],
    },
    {
        id: 'submissions',
        title: 'Submissions',
        type: 'aside',
        icon: 'heroicons_outline:cloud-arrow-up',
        children: [
            {
                id: 'submissions.list',
                title: 'Submissions',
                type: 'basic',
                icon: 'heroicons_outline:cloud-arrow-up',
                link: '/submissions/data',
                exactMatch: true
            },
            {
                id: 'submissions.create',
                title: 'Create Submission',
                type: 'basic',
                icon: 'heroicons_outline:plus-circle',
                link: '/submissions/data/create',
                exactMatch: true
            },
        ],
    },
];

export const compactNavigation: FuseNavigationItem[] = defaultNavigation;
export const futuristicNavigation: FuseNavigationItem[] = defaultNavigation;
export const horizontalNavigation: FuseNavigationItem[] = defaultNavigation;
