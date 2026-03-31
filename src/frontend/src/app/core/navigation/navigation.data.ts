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
                id: 'exams.dashboard',
                title: 'Dashboard',
                type: 'basic',
                icon: 'heroicons_outline:chart-pie',
                link: '/exams/dashboard',
            },
            {
                id: 'exams.list',
                title: 'Exams',
                type: 'basic',
                icon: 'heroicons_outline:academic-cap',
                link: '/exams/data',
            },
            {
                id: 'exams.create',
                title: 'Create Exam',
                type: 'basic',
                icon: 'heroicons_outline:plus-circle',
                link: '/exams/create',
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
                id: 'submissions.dashboard',
                title: 'Dashboard',
                type: 'basic',
                icon: 'heroicons_outline:chart-pie',
                link: '/submissions/dashboard',
            },
            {
                id: 'submissions.list',
                title: 'Submissions',
                type: 'basic',
                icon: 'heroicons_outline:cloud-arrow-up',
                link: '/submissions/data',
            },
            {
                id: 'submissions.create',
                title: 'Create Submission',
                type: 'basic',
                icon: 'heroicons_outline:plus-circle',
                link: '/submissions/create',
            },
        ],
    },
    
];

export const compactNavigation: FuseNavigationItem[] = defaultNavigation;
export const futuristicNavigation: FuseNavigationItem[] = defaultNavigation;
export const horizontalNavigation: FuseNavigationItem[] = defaultNavigation;
